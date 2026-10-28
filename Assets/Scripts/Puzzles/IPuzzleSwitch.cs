// Qualsevol element que es pot activar en un trencaclosques (palanca, placa de pressió...).
// Un PuzzleMover pot escoltar-ne uns quants i moure una porta o una plataforma quan s'activen
public interface IPuzzleSwitch
{
    bool IsActive { get; }
}
