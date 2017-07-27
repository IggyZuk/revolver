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
		if (Input.GetMouseButtonDown(0)) {
			inputModel.startDragPos = Input.mousePosition;
			inputModel.isDragging = true;
		} else if (Input.GetMouseButtonUp(0)) {
			inputModel.isDragging = false;
			if (inputModel.shootAction != null) inputModel.shootAction(Input.mousePosition);
		}

		if (inputModel.isDragging) {
			inputModel.currentDragPos = Input.mousePosition;

			Vector3 startPosWorld = Camera.main.ScreenToWorldPoint(inputModel.startDragPos);
			Vector3 endPosWorld = Camera.main.ScreenToWorldPoint(inputModel.currentDragPos);
			inputModel.distance = (startPosWorld - endPosWorld).magnitude;
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
