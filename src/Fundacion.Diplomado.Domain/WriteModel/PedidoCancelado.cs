using System;

namespace Fundacion.Diplomado.Domain.WriteModel
{
    public class PedidoCancelado : IEvent
    {
        public string ClienteId { get; }
        public Guid PedidoId { get; }

        public PedidoCancelado(string clienteId, Guid pedidoId)
        {
            ClienteId = clienteId;
            PedidoId = pedidoId;
        }
    }
}