namespace Eternity.WpfApp
{
    enum Rotation
    {
        None = 0,
        Ninety = 1,
        OneEighty = 2,
        TwoSeventy = 3
    };
    record class Placement(int PieceIndex, Rotation rotation);
}
