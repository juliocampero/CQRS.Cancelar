namespace Fundacion.Diplomado.Domain.WriteModel
{
    public interface IEntregarPizzas
    {
        void EntregarLaPizza(PedidoCommand command);
        void CancelarLaPizza(CancelarPedidoCommand command); // El nuevo comando
    }
}