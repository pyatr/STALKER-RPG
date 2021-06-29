using UnityEngine;

public class FocusOnGameObject : MonoBehaviour
{
    private World world;
    private GameObject focusObject;
    private Vector2 offset = Vector2.zero;
    private Vector2 offsetLimit = new Vector2(8, 6);

    public GameObject FocusObject { get { return focusObject; } }
    public Vector2 Offset { get { return offset; } }
    public float minCameraDistance = 0.5f;
    public float maxCameraDistance = 7.0f;

    public void Focus()
    {
        if (focusObject != null)
            Camera.main.gameObject.transform.localPosition = new Vector3(focusObject.transform.localPosition.x + offset.x, focusObject.transform.localPosition.y + offset.y, transform.localPosition.z);
    }

    public void ChangeFocusObject(GameObject newFocusObject)
    {
        focusObject = newFocusObject;
        offset = Vector2.zero;
    }

    public void ModOffset(Vector2 offsetOffset)
    {
        offset += offsetOffset;
        //offset = new Vector2(Mathf.Clamp(offset.x, -offsetLimit.x, offsetLimit.x), Mathf.Clamp(offset.y, -offsetLimit.y, offsetLimit.y));
    }

    public void SetOffset(Vector2 newOffset)
    {
        offset = newOffset;
        //offset = new Vector2(Mathf.Clamp(offset.x, -offsetLimit.x, offsetLimit.x), Mathf.Clamp(offset.y, -offsetLimit.y, offsetLimit.y));
    }

    public void ResetOffset()
    {
        offset = Vector2.zero;
    }

    private void Start()
    {
        world = World.GetInstance();
        Focus();
    }

    private void Update()
    {
        if (world.CurrentUiMode == InGameUI.Interface)
        {
            if (Input.GetKey(KeyCode.KeypadMinus))
                Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize + 0.045f, minCameraDistance, maxCameraDistance);
            if (Input.GetKey(KeyCode.KeypadPlus))
                Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - 0.045f, minCameraDistance, maxCameraDistance);
            if (Input.mouseScrollDelta.y != 0.0f)
                Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - Input.mouseScrollDelta.y / 5, minCameraDistance, maxCameraDistance);
            if (focusObject == null)
                return;
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                if (Input.GetMouseButton(1))
                {
                    ResetOffset();
                    return;
                }
                Vector2 difference = transform.localPosition - world.groundCursor.localPosition;
                float directionX = 0;
                float directionY = 0;
                if (Mathf.Abs(difference.x) > Game.Instance.cellSize.x)
                    directionX = (difference.x / Mathf.Abs(difference.x));
                if (Mathf.Abs(difference.y) > Game.Instance.cellSize.y)
                    directionY = (difference.y / Mathf.Abs(difference.y));
                Vector2 direction = new Vector2(directionX, directionY);
                if (Game.Instance.PointIsOnScreen((Vector2)world.Player.transform.position + Game.Instance.cellSize * direction))
                    ModOffset(Game.Instance.cellSize * direction * -1 / 16);
            }
            Focus();
        }
    }
}