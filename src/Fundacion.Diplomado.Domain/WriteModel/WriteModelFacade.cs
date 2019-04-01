namespace Fundacion.Diplomado.Domain.WriteModel
{
    public class WriteModelFacade : IHandleCommand<PedidoCommand>, IHandleCommand<CancelarPedidoCommand>
    {
        public IEntregarPizzas PedidoStore { get; }

        public WriteModelFacade(IEntregarPizzas bookingStore)
        {
            this.PedidoStore = bookingStore;
        }

        public void Handle(PedidoCommand command)
        {
            this.PedidoStore.EntregarLaPizza(command);
        }

        public void Handle(CancelarPedidoCommand command)
        {
            this.PedidoStore.CancelarLaPizza(command);
        }
    }

    public interface IHandleCommand<T>
    {
        void Handle(T command);
    }
}