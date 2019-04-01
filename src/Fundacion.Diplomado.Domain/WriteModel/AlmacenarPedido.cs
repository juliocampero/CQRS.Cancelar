using System;

namespace Fundacion.Diplomado.Domain.WriteModel
{
    public class AlmacenarPedido : IEntregarPizzas
    {
        private readonly IPedidoRepository pedidoRepository;
        private readonly IClienteRepository clienteRepository;
        private readonly IPublishEvents publicarEventos;

        public AlmacenarPedido(IPedidoRepository pedidoRepository, IClienteRepository clienteRepository,  IPublishEvents publicarEventos)
        {
            this.pedidoRepository = pedidoRepository;
            this.clienteRepository = clienteRepository;
            this.publicarEventos = publicarEventos;
        }

        public void EntregarLaPizza(PedidoCommand command)
        {
            if (!this.clienteRepository.YaEsCliente(command.ClienteId))
            {
                this.clienteRepository.CrearClient(command.ClienteId);    
            }

            Guid guid = Guid.NewGuid();
            var enviarPedido = new Pedido(guid, command.ClienteId, command.PizzeriaId, command.PizzaNombre, command.Cantidad, command.FechaDeEntrega);
            this.pedidoRepository.Grabar(enviarPedido);
            command.Guid = guid;    // Setear para saber el guid del pedido. Improve later
            this.pedidoRepository.Grabar(command);

            // we could enrich the event from here
            var pizzaEnviada = new PizzaEnviada(guid, command.PizzaNombre, command.PizzeriaId, command.ClienteId, command.Cantidad, command.FechaDeEntrega);
            this.publicarEventos.PublishTo(pizzaEnviada);
        }

        public void CancelarLaPizza(CancelarPedidoCommand cancelBookingCommand)
        {
            var pedido = this.pedidoRepository.GetPedido(cancelBookingCommand.ClienteId, cancelBookingCommand.PedidoId);
            if (pedido.EsParaCliente(cancelBookingCommand.ClienteId))
            {
                // We cancel the request
                pedido.Cancelar();

                // And save its updated version
                this.pedidoRepository.Actualizar(pedido);

                // HERE, WE INSTANTIATE AND PUBLISH A BRAND NEW EVENT --------------
                var pedidoCancelado = new PedidoCancelado(pedido.ClienteId, pedido.PedidoId);
                this.publicarEventos.PublishTo(pedidoCancelado);
            }
            else
            {
                throw new InvalidOperationException("Can't cancel a delivery for another client.");
            }
        }
    }
}
