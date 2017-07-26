using UnityEngine;

public class InputController
{
    public class InputModel
    {
        public Vector2 startDragPos;
        public Vector2 currentDragPos;
        public bool isDragging;
        public System.Action<Vector2> shootAction;
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
            if (inputModel.shootAction != null) inputModel.shootAction(Input.mousePosition);
        }

        if (inputModel.isDragging)
        {
            inputModel.currentDragPos = Input.mousePosition;
        }
    }
}
