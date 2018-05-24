using UnityEngine;

public class InputController
{
	public class InputModel
	{
		public Vector2 startDragPos;
		public Vector2 currentDragPos;
		public bool isDragging;
		public System.Action<Vector2> shootAction;
		public float distance;
	}

	InputModel inputModel = new InputModel();

	public InputModel GetModel()
	{
		return inputModel;
	}

	public void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			inputModel.startDragPos = Input.mousePosition;
			inputModel.isDragging = true;
		}
		else if (Input.GetMouseButtonUp(0))
		{
			inputModel.isDragging = false;
			//if (inputModel.shootAction != null) inputModel.shootAction(Input.mousePosition);
		}

		if (inputModel.isDragging)
		{
			inputModel.currentDragPos = Input.mousePosition;

			Vector3 startPosWorld = Camera.main.ScreenToWorldPoint(inputModel.startDragPos);
			Vector3 endPosWorld = Camera.main.ScreenToWorldPoint(inputModel.currentDragPos);
			inputModel.distance = (startPosWorld - endPosWorld).magnitude;
		}

        float speed = Input.GetKey(KeyCode.LeftShift) ? 1f : 0.01f;

        if (Input.GetKey(KeyCode.LeftArrow))
        { 
            Vector2 v = (inputModel.currentDragPos - inputModel.startDragPos).normalized;
            Vector2 n = new Vector2(-v.y, v.x) * speed;
            inputModel.currentDragPos += n;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            Vector2 v = (inputModel.currentDragPos - inputModel.startDragPos).normalized;
            Vector2 n = new Vector2(v.y, -v.x) * speed;
            inputModel.currentDragPos += n;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        { 
            if (inputModel.shootAction != null) inputModel.shootAction(inputModel.currentDragPos);
        }
	}

	public Position GetShootDir()
	{
		Vector3 startPosWorld = Camera.main.ScreenToWorldPoint(inputModel.startDragPos);
		Vector3 endPosWorld = Camera.main.ScreenToWorldPoint(inputModel.currentDragPos);

		Position startPos = new Position(startPosWorld.x, startPosWorld.z);
		Position endPos = new Position(endPosWorld.x, endPosWorld.z);

		return (startPos - endPos).Normalize();
	}
}
