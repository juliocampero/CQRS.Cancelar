using System;

namespace Fundacion.Diplomado.Domain.WriteModel
{
    public interface IPedidoRepository
    {
        void Grabar(Pedido pedido);
        void Grabar(ICommand comando);
        Pedido GetPedido(string clienteId, Guid pedidoId);
        void Actualizar(Pedido pedido);
    }
}