using System;
using Xamarin.Forms.Platform.iOS;
using Foundation;
using UIKit;
using Xamarin.Forms;
using CoreGraphics;
using KeyboardOverlap.Forms.Plugin.iOSUnified;
using System.Diagnostics;

[assembly: ExportRenderer(typeof(Page), typeof(KeyboardOverlapRenderer))]
namespace KeyboardOverlap.Forms.Plugin.iOSUnified
{
    [Preserve(AllMembers = true)]
    public class KeyboardOverlapRenderer : PageRenderer
    {
        NSObject _keyboardShowObserver;
        NSObject _keyboardHideObserver;
        NSObject _keyboardFrameChangeObserver;
        private bool _pageWasShiftedUp;
        private double _activeViewBottom;
        private bool _isKeyboardShown;
        private static OverlapType overlapType;

        public enum OverlapType
        {
            ShiftUp = 0,
            Collapse
        };

        public static void Init(OverlapType type)
        {
            var now = DateTime.Now;
            overlapType = type;
            Debug.WriteLine("Keyboard Overlap plugin initialized {0}", now);
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            var page = Element as ContentPage;

            if (page != null)
            {
                var contentScrollView = page.Content as ScrollView;

                if (contentScrollView != null)
                    return;

                RegisterForKeyboardNotifications();
            }
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);

            UnregisterForKeyboardNotifications();
        }

        void RegisterForKeyboardNotifications()
        {
            if (_keyboardShowObserver == null)
                _keyboardShowObserver = NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillShowNotification, OnKeyboardShow);
            if (_keyboardHideObserver == null)
                _keyboardHideObserver = NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillHideNotification, OnKeyboardHide);
            if (_keyboardFrameChangeObserver == null)
                _keyboardFrameChangeObserver = NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillChangeFrameNotification, OnKeyboardChangeFrame);
        }

        void UnregisterForKeyboardNotifications()
        {
            _isKeyboardShown = false;
            if (_keyboardShowObserver != null)
            {
                NSNotificationCenter.DefaultCenter.RemoveObserver(_keyboardShowObserver);
                _keyboardShowObserver.Dispose();
                _keyboardShowObserver = null;
            }

            if (_keyboardHideObserver != null)
            {
                NSNotificationCenter.DefaultCenter.RemoveObserver(_keyboardHideObserver);
                _keyboardHideObserver.Dispose();
                _keyboardHideObserver = null;
            }

            if (_keyboardFrameChangeObserver != null)
            {
                NSNotificationCenter.DefaultCenter.RemoveObserver(_keyboardFrameChangeObserver);
                _keyboardFrameChangeObserver.Dispose();
                _keyboardFrameChangeObserver = null;
            }
        }

        protected virtual void OnKeyboardShow(NSNotification notification)
        {
            if (!IsViewLoaded || _isKeyboardShown)
                return;

            _isKeyboardShown = true;
            var activeView = View.FindFirstResponder();

            if (activeView == null)
                return;

            var keyboardFrame = UIKeyboard.FrameEndFromNotification(notification);
            _activeViewBottom = activeView.GetViewRelativeBottom(View);

            switch (overlapType)
            {
                case OverlapType.ShiftUp:
                    ShiftPageUp(keyboardFrame.Height);
                    break;
                case OverlapType.Collapse:
                    CollapseKeyboard(keyboardFrame.Height);
                    break;
                default:
                    break;
            }
        }

        private void OnKeyboardHide(NSNotification notification)
        {
            if (!IsViewLoaded)
                return;

            _isKeyboardShown = false;
            var keyboardFrame = UIKeyboard.FrameEndFromNotification(notification);

            if (_pageWasShiftedUp)
            {
                ResetPagePositon(keyboardFrame.Height);
            }
        }

        protected virtual void OnKeyboardChangeFrame(NSNotification notification)
        {
            if (!_isKeyboardShown)
                return;

            var activeView = View.FindFirstResponder();

            if (activeView == null)
                return;

            var keyboardFrame = UIKeyboard.FrameEndFromNotification(notification);

            switch(overlapType)
            {
                case OverlapType.ShiftUp:
                    ShiftPageUp(keyboardFrame.Height);
                    break;
                case OverlapType.Collapse:
                    CollapseKeyboard(keyboardFrame.Height);
                    break;
                default:
                    break;
            }
        }

        private void ShiftPageUp(nfloat keyboardHeight)
        {
            var pageFrame = Element.Bounds;

            var newY = UIApplication.SharedApplication.KeyWindow.Frame.Y + CalculateShiftByAmount(pageFrame.Height, keyboardHeight, _activeViewBottom);
            Element.LayoutTo(new Rectangle(pageFrame.X, newY,
                pageFrame.Width, pageFrame.Height));

            _pageWasShiftedUp = true;
        }

        private void CollapseKeyboard(nfloat keyboardHeight)
        {
            var pageFrame = Element.Bounds;

            Element.LayoutTo(new Rectangle(pageFrame.X, pageFrame.Y,
                pageFrame.Width, UIApplication.SharedApplication.KeyWindow.Frame.Height - keyboardHeight));

            _pageWasShiftedUp = true;
        }

        private void ResetPagePositon(nfloat keyboardHeight)
        {
            var pageFrame = Element.Bounds;

            Element.LayoutTo(new Rectangle(pageFrame.X, UIApplication.SharedApplication.KeyWindow.Frame.Y,
                pageFrame.Width, UIApplication.SharedApplication.KeyWindow.Frame.Height));

            _pageWasShiftedUp = false;
        }

        private double CalculateShiftByAmount(double pageHeight, nfloat keyboardHeight, double activeViewBottom)
        {
            return (pageHeight - activeViewBottom) - keyboardHeight;
        }
    }
}