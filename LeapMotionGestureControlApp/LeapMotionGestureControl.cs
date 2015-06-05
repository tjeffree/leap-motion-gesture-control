using System;
using Leap;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class LeapMotionGestureControlListener : Listener
{
    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, UIntPtr dwExtraInfo);

    private const uint MOUSEEVENTF_SCROLL = 0x0800;

    private int scroll_speed = 15;
    private LastCircleGesture lastCircleGesture = null;

    private bool sendingKey = false;
    private System.Timers.Timer tmr;

    public override void OnFrame(Controller controller)
    {

        // Get the most recent frame
        Frame frame = controller.Frame();

        if (!frame.Hands.IsEmpty)
        {
            for (int g = 0; g < frame.Gestures().Count; g++)
            {

                switch (frame.Gestures()[g].Type)
                {
                    case Gesture.GestureType.TYPE_CIRCLE:
                        handleScroll(frame, new CircleGesture(frame.Gestures()[g]));
                        return;

                    case Gesture.GestureType.TYPE_KEY_TAP:
                        handleTap(frame, new KeyTapGesture(frame.Gestures()[g]));
                        return;

                    case Gesture.GestureType.TYPE_SWIPE:
                        handleSwipe(frame, new SwipeGesture(frame.Gestures()[g]));
                        return;
                }

            }
        }

    }

    private void handleScroll(Frame frame, CircleGesture circle) {

        int speed           = scroll_speed;
        Boolean isClockwise = checkClockwise(circle);
        float scroll_distance;

        LastCircleGesture newCircleGesture = new LastCircleGesture();

        newCircleGesture.circle      = circle;
        newCircleGesture.isClockwise = isClockwise;

        if (lastCircleGesture == null || isClockwise != lastCircleGesture.isClockwise || circle.Id != lastCircleGesture.circle.Id)
        {
            lastCircleGesture = newCircleGesture;
            return;
        }

        scroll_distance = getScrollDistance(frame, newCircleGesture);

        if (scroll_distance != 0)
        {
            if (circle.State != Leap.Gesture.GestureState.STATE_STOP)
            {
                mouse_event(MOUSEEVENTF_SCROLL, (uint)0, (uint)0, (uint)scroll_distance, (UIntPtr)0);
            }
        }

        lastCircleGesture = (circle.State == Leap.Gesture.GestureState.STATE_STOP) ? null : newCircleGesture;
    }

    private Boolean checkClockwise(CircleGesture circle)
    {
        if (circle.Pointable.Direction.AngleTo(circle.Normal) <= Math.PI / 2)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private float getScrollDistance(Frame frame, LastCircleGesture gesture)
    {
        float speed = gesture.circle.Radius / 2 * frame.Fingers.Count;

        if (speed < 10) { speed = 10; }

        float scroll_distance = (gesture.circle.Progress - lastCircleGesture.circle.Progress) * 10 * speed;

        // Make sure the distance isn't stupid - or too small
        if (scroll_distance > 100 || scroll_distance <= 1)
        {
            return 0;
        }

        // If it's clockwise we need the negative
        if (gesture.isClockwise)
        {
            scroll_distance = -scroll_distance;
        }

        return scroll_distance;
    }

    private void handleTap(Frame frame, KeyTapGesture tap)
    {
        if (!sendingKey)
        {
            SendKeys.SendWait("{DOWN}");

        }

        setKeyTimer();
    }

    private void handleSwipe(Frame frame, SwipeGesture swipe)
    {
        if (!sendingKey)
        {
            string keyCombo = "";
            
            bool isHorizontal = Math.Abs(swipe.Direction.x) > Math.Abs(swipe.Direction.y);

            if (isHorizontal)
            {
                float swipeLength = Math.Abs(swipe.StartPosition.x) - Math.Abs(swipe.Position.x);
                
                if (swipeLength <= 100 && swipeLength >= -100)
                {
                    if (swipe.Direction.x > 0.5)
                    {
                        keyCombo = "^({TAB})";
                    }
                    else if (swipe.Direction.x < -0.5)
                    {
                        keyCombo = "^(+({TAB}))";
                    }
                }
            }
            else
            {
                float swipeLength = Math.Abs(swipe.StartPosition.y) - Math.Abs(swipe.Position.y);
                
                if (swipeLength <= 140 && swipeLength >= -140)
                {
                    if (swipe.Direction.y > 0.5)
                    {
                        keyCombo = "{HOME}";
                    }
                    else if (swipe.Direction.y < -0.5)
                    {
                        keyCombo = "{END}";
                    }
                }
            }

            SendKeys.SendWait(keyCombo);

        }

        setKeyTimer();
    }

    private void setKeyTimer()
    {
        sendingKey = true;

        if (tmr == null)
        {
            tmr = new System.Timers.Timer();
            tmr.Interval = 500;
            tmr.Elapsed += new System.Timers.ElapsedEventHandler(timerHandler);
        }

        tmr.Start();

    }

    private void timerHandler(object sender, EventArgs e)
    {
        sendingKey = false;
        tmr.Stop();
    }

}

class LastCircleGesture
{
    public Boolean isClockwise;
    public CircleGesture circle;
}

class LeapMotionGestureControl
{

    LeapMotionGestureControlListener listener;
    Controller controller;

    public LeapMotionGestureControl()
    {
        // Set up listener and controller
        listener = new LeapMotionGestureControlListener();
        controller = new Controller();

        // Allow background frames, otherwise.. well, it's useless
        controller.SetPolicy(Controller.PolicyFlag.POLICY_BACKGROUND_FRAMES);

        // Watch for circles
        controller.EnableGesture(Gesture.GestureType.TYPE_CIRCLE);
        controller.EnableGesture(Gesture.GestureType.TYPE_KEY_TAP);
        controller.EnableGesture(Gesture.GestureType.TYPE_SWIPE);

        // Add on the listener
        controller.AddListener(listener);
    }

    public void destroy()
    {
        // Destroy the things
        controller.RemoveListener(listener);
        controller.Dispose();
    }

}
