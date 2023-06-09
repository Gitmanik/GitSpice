using Godot;

public partial class ElementPort : Button
{
    public PortData Data;

    public Element ParentElement;

    public Vector2 Centroid => GlobalPosition + CalculateCentroid();

    private Vector2 CalculateCentroid()
    {
        //https://gamedev.stackexchange.com/questions/57211/how-to-get-the-center-of-a-rotated-rectangle-regardless-of-point-of-rotation
        Vector2 localCentroid = new Vector2();
        Vector2 localCenter = (GetGlobalRect().Size / 2);

        localCentroid.X = localCenter.X * Mathf.Cos(ParentElement.Rotation) - localCenter.Y * Mathf.Sin(ParentElement.Rotation);
        localCentroid.Y = localCenter.X * Mathf.Sin(ParentElement.Rotation) + localCenter.Y * Mathf.Cos(ParentElement.Rotation);
        return localCentroid;
    }

    public override void _Pressed()
    {
        UserInputController.Instance.ConnectClicked(this);
    }
}
