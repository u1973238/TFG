// Objectes que reaccionen quan el fil de balancejar-se (el que es dispara amb la mira) hi arriba.
// Per exemple, una palanca que l'aranya estira des de lluny
public interface IWebTarget
{
    // El fil acaba d'arribar a l'objecte (el moment del «clac» i de la nuvolada)
    void OnWebArrived(SpiderSwingingSimple web);
}
