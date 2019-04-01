using System;

namespace Fundacion.Diplomado.Domain.WriteModel
{
    //Note: since we 'TDD as if you meant it', the newly created command sits aside the test(we'll move it in a second step).
    public class CancelarPedidoCommand : ICommand
    {
        public Guid PedidoId { get; }
        public string ClienteId { get; }

        public CancelarPedidoCommand(Guid pedidoId, string clienteId)
        {
            this.PedidoId = pedidoId;
            this.ClienteId = clienteId;
        }
    }
}