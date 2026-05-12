using UnityEngine;
using UnityEngine.EventSystems;

public static class TouchPointerInput
{
    private static int activeFingerId = -1;
    private static int cachedFrame = -1;
    private static Vector2 cachedScreenPosition;
    private static bool cachedBegan;
    private static bool cachedHeld;
    private static bool cachedEnded;
    private static bool cachedHasPointer;
    private static bool mouseActive;

    public static bool TryGetPrimaryPointer(out Vector2 screenPosition, out bool began, out bool held, out bool ended)
    {
        RefreshCache();

        screenPosition = cachedScreenPosition;
        began = cachedBegan;
        held = cachedHeld;
        ended = cachedEnded;
        return cachedHasPointer;
    }

    public static bool WasPrimaryPointerReleased()
    {
        return TryGetPrimaryPointer(out _, out _, out _, out bool ended) && ended;
    }

    public static bool WasPrimaryPointerTapped()
    {
        return TryGetPrimaryPointer(out _, out bool began, out _, out bool ended) &&
               (began || ended);
    }

    private static bool TryGetTouchPointer(out Vector2 screenPosition, out bool began, out bool held, out bool ended)
    {
        screenPosition = Vector2.zero;
        began = false;
        held = false;
        ended = false;

        if (activeFingerId == -1)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch candidate = Input.GetTouch(i);
                if (candidate.phase != TouchPhase.Began || IsPointerOverUi(candidate.fingerId))
                {
                    continue;
                }

                activeFingerId = candidate.fingerId;
                screenPosition = candidate.position;
                began = true;
                held = true;
                return true;
            }

            return false;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.fingerId != activeFingerId)
            {
                continue;
            }

            screenPosition = touch.position;
            ended = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            held = !ended;

            if (ended)
            {
                activeFingerId = -1;
            }

            return true;
        }

        activeFingerId = -1;
        return false;
    }

    private static void RefreshCache()
    {
        if (cachedFrame == Time.frameCount)
        {
            return;
        }

        cachedFrame = Time.frameCount;
        cachedScreenPosition = Vector2.zero;
        cachedBegan = false;
        cachedHeld = false;
        cachedEnded = false;

        cachedHasPointer = Input.touchCount > 0
            ? TryGetTouchPointer(out cachedScreenPosition, out cachedBegan, out cachedHeld, out cachedEnded)
            : TryGetMousePointer(out cachedScreenPosition, out cachedBegan, out cachedHeld, out cachedEnded);
    }

    private static bool TryGetMousePointer(out Vector2 screenPosition, out bool began, out bool held, out bool ended)
    {
        screenPosition = Input.mousePosition;
        if (Input.GetMouseButtonDown(0))
        {
            mouseActive = !IsPointerOverUi();
        }

        began = Input.GetMouseButtonDown(0) && mouseActive;
        ended = Input.GetMouseButtonUp(0) && mouseActive;
        held = Input.GetMouseButton(0) && mouseActive && !ended;

        if (Input.GetMouseButtonUp(0))
        {
            mouseActive = false;
        }

        return began || held || ended;
    }

    private static bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private static bool IsPointerOverUi(int fingerId)
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(fingerId);
    }
}
