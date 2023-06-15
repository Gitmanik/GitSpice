using Gitmanik.Controllers;
using Godot;

public partial class ElementPort : Button
{
    public string PortId;

    public Element ParentElement;

    public Vector2 Centroid => ParentElement.Position + Position.Rotated(ParentElement.Rotation) + CalculateCentroid();

    private Vector2 CalculateCentroid()
    {
        //https://gamedev.stackexchange.com/questions/57211/how-to-get-the-center-of-a-rotated-rectangle-regardless-of-point-of-rotation
        Vector2 localCentroid = new Vector2();
        Vector2 localCenter = (GetRect().Size / 2);

        localCentroid.X = localCenter.X * Mathf.Cos(ParentElement.Rotation) - localCenter.Y * Mathf.Sin(ParentElement.Rotation);
        localCentroid.Y = localCenter.X * Mathf.Sin(ParentElement.Rotation) + localCenter.Y * Mathf.Cos(ParentElement.Rotation);

        return localCentroid;
    }

    public override void _Draw()
    {
        if (AppController.Settings.Data.DebugDrawIds)
            DrawString(ThemeDB.FallbackFont, Vector2.Zero, PortId, HorizontalAlignment.Center);
    }

    public override void _Pressed()
    {
        // Stop moving when Port is clicked (Pole having port in the middle fix)
        if (ParentElement.Selected)
        {
            ParentElement.Selected = false;
            Element.IsCurrentlyMoving = false;
            this.ButtonPressed = false;
            return;
        }

        UserInputController.Instance.ElementPortClicked(this);
    }
}
