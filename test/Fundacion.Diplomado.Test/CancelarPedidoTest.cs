using Microsoft.VisualStudio.TestTools.UnitTesting;
using NFluent;
using Fundacion.Diplomado.Infra.WriteModel;
using Fundacion.Diplomado.Infra;
using Fundacion.Diplomado.Infra.MessageBus;
using Fundacion.Diplomado.Domain.WriteModel;
using System.Linq;
using Fundacion.Diplomado.Tests;
using Fundacion.Diplomado.Infra.ReadModel.Adapters;
using Fundacion.Diplomado.Domain.ReadModel;

namespace Fundacion.Diplomado.Domain.Test
{
    [TestClass]
    public class CancelarPedidoTest
    {
        [TestMethod]
        public void Debe_actualizar_pedidoEngine_cuando_cancela_pedidoCommand_es_enviado()
        {
            var pedidoEngine = new PedidoYClientesRepository();
            var bus = new FakeBus();
            CompositionRootHelper.BuildTheWriteModelHexagon(pedidoEngine, pedidoEngine, bus, bus);

            var pizzeriaId = 1;
            var cantidad = 1;
            var clienteId = "julio.campero@jalasoft.com";
            var pedidoCommand = new PedidoCommand(
                clienteId: clienteId, 
                pizzaNombre: "Malcriada", 
                pizzeriaId: pizzeriaId, 
                cantidad: cantidad, 
                fechaDeEntrega: Constants.MyFavoriteSaturdayIn2019);

            bus.Send(pedidoCommand);

            Check.That(pedidoEngine.GetPedidoDe(clienteId)).HasSize(1);
            var deliveryGuid = pedidoEngine.GetPedidoDe(clienteId).First().PedidoId;

            var cancelDeliveryCommand = new CancelarPedidoCommand(deliveryGuid, clienteId);
            bus.Send(cancelDeliveryCommand);

            // Pedido is still there, but canceled
            Check.That(pedidoEngine.GetPedidoDe(clienteId)).HasSize(1);
            Check.That(pedidoEngine.GetPedidoDe(clienteId).First().EsCancelado).IsTrue();
        }

        [TestMethod]
        public void DebeActualizar_readmodel_usuario_reservacion_cuando_CancelarPedidoCommand_is_enviadot()
        {
            var pedidoEngine = new PedidoYClientesRepository();
            var bus = new FakeBus(synchronousPublication: true);
            CompositionRootHelper.BuildTheWriteModelHexagon(pedidoEngine, pedidoEngine, bus, bus);

            var pizzeriasYPizzasAdapter = new PizzeriasYPizzasAdapter(Constants.RelativePathForPizzaIntegrationFiles, bus);
            pizzeriasYPizzasAdapter.LoadAllPizzaFiles();
            var reservationAdapter = new ReservaAdapter(bus);
            CompositionRootHelper.BuildTheReadModelHexagon(pizzeriasYPizzasAdapter, pizzeriasYPizzasAdapter, reservationAdapter, bus);

            var clienteId = "julio.campero@jalasoft.com";
            Check.That(reservationAdapter.GetReservacionesDe(clienteId)).IsEmpty();

            var pizzeriaId = 2;
            var cantidad = 2;
            var pedidoCommand = new PedidoCommand(clienteId: clienteId, pizzeriaId: pizzeriaId, pizzaNombre: "Peperoni", cantidad: 2, fechaDeEntrega: Constants.MyFavoriteSaturdayIn2019);

            bus.Send(pedidoCommand);

            var pedidoGuid = pedidoEngine.GetPedidoDe(clienteId).First().PedidoId;

            Check.That(reservationAdapter.GetReservacionesDe(clienteId)).HasSize(1);

            var reservation = reservationAdapter.GetReservacionesDe(clienteId).First();
            Check.That(reservation.Cantidad).IsEqualTo(cantidad);
            Check.That(reservation.PizzeriaId).IsEqualTo(pizzeriaId);

            var cancelCommand = new CancelarPedidoCommand(pedidoGuid, clienteId);
            bus.Send(cancelCommand);

            Check.That(reservationAdapter.GetReservacionesDe(pedidoGuid, clienteId)).HasSize(0);
        }

        [TestMethod]
        public void Debe_actualizar_buscar_pizza_cuando_cancelaPedidoCommand_es_enviado()
        {
            // Initialize Read-model side
            var bus = new FakeBus(synchronousPublication: true);
            var pizzeriasAdapter = new PizzeriasYPizzasAdapter(Constants.RelativePathForPizzaIntegrationFiles, bus);
            var pedidoAdapter = new ReservaAdapter(bus);
            pizzeriasAdapter.LoadPizzaFile("Malcriada-availabilities.json");

            // Initialize Write-model side
            var bookingRepository = new PedidoYClientesRepository();
            CompositionRootHelper.BuildTheWriteModelHexagon(bookingRepository, bookingRepository, bus, bus);

            var readFacade = CompositionRootHelper.BuildTheReadModelHexagon(pizzeriasAdapter, pizzeriasAdapter, pedidoAdapter, bus);

            // Search Pizzas availabilities
            var fechaEntrega = Constants.MyFavoriteSaturdayIn2019;

            var searchQuery = new BuscarPedidoOpciones(fechaEntrega, direccion: "Cercado", nombrePizza: "peperoni", cantidad: 2);
            var pedidoOpciones = readFacade.BuscarPedidoOpciones(searchQuery);

            // We should get 1 pedido option with 13 available pizzas in it.
            Check.That(pedidoOpciones).HasSize(1);

            var pedidoOpcion = pedidoOpciones.First();
            var initialPizzaNumbers = 13;
            Check.That(pedidoOpcion.DisponiblesPizzasConPrecios).HasSize(initialPizzaNumbers);

            // Now, let's request that pizza!
            var firstPizzaOfThisPedidoOption = pedidoOpcion.DisponiblesPizzasConPrecios.First();
            var clientId = "julio.campero@jalasoft.com";
            var pedidoCommand = new PedidoCommand(clienteId: clientId, pizzeriaId: pedidoOpcion.Pizzeria.Identificador, pizzaNombre: "MalcriadaC", cantidad: firstPizzaOfThisPedidoOption.Cantidad, fechaDeEntrega: fechaEntrega);

            // We send the PedirPizza command
            bus.Send(pedidoCommand);

            // We check that both the PedidoRepository (Write model) and the available pizzas (Read model) have been updated.
            Check.That(bookingRepository.GetPedidoDe(clientId).Count()).IsEqualTo(1);
            var pedidoId = bookingRepository.GetPedidoDe(clientId).First().PedidoId;

            // Fetch pizzas availabilities now. One pizza should have disappeared from the search result
            pedidoOpciones = readFacade.BuscarPedidoOpciones(searchQuery);
            Check.That(pedidoOpciones).HasSize(1);
            Check.That(pedidoOpcion.DisponiblesPizzasConPrecios).As("available matching pizzas").HasSize(initialPizzaNumbers - 1);
        }
    }
}
