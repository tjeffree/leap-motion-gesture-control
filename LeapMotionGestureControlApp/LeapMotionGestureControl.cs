using System;
using Leap;
using System.Runtime.InteropServices;

class LeapMotionGestureControlListener : Listener
{

    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, UIntPtr dwExtraInfo);

    private const uint MOUSEEVENTF_SCROLL= 0x0800;

    private Object gestureLock = new Object();

    private int scroll_speed = 15;
    private LastCircleGesture lastCircleGesture = null;

    public override void OnFrame(Controller controller)
    {
        lock (gestureLock)
        {
            // Get the most recent frame
            Frame frame = controller.Frame();

            if (!frame.Hands.IsEmpty)
            {
                for (int g = 0; g < frame.Gestures().Count; g++)
                {
                    if (frame.Gestures()[g].Type == Gesture.GestureType.TYPE_CIRCLE)
                    {
                        handleScroll(frame, new CircleGesture(frame.Gestures()[g]));

                        // At the first circle gesture, stop here
                        break;
                    }
                }
            }

        }
    }

    public void handleScroll(Frame frame, CircleGesture circle) {

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
