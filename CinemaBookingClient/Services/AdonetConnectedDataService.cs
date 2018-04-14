using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using CinemaBookingClient.Models;
using CinemaBookingClient.Models.DataModel;
using Microsoft.Extensions.Configuration;

namespace CinemaBookingClient.Services
{
    public class AdonetConnectedDataService : ICinemaDataService
    {
        private IConfiguration configuration;
        private string connectionStr;

        public AdonetConnectedDataService(IConfiguration configuration)
        {
            this.configuration = configuration;
            connectionStr = configuration.GetConnectionString("DefaultConnection");
        }

        public int CancelTickets(string userId, int cinemaHallId, int seanceId, IEnumerable<Position> removeSeats)
        {
            throw new NotImplementedException();
        }

        public Customer CreateCustomer(string aspNetUserId)
        {
            Customer customer = null;
            using (var conn = new SqlConnection(connectionStr))
            {
                var cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"insert into Customers (AspNetUsersId) 
                    values (@AspNetUsersId); 
                    SELECT Id
                    FROM Customers WHERE(Id = SCOPE_IDENTITY())";
                cmd.Parameters.AddWithValue("@AspNetUsersId", aspNetUserId);
                cmd.Connection.Open();
                int id = (int)cmd.ExecuteScalar();

                customer = new Customer
                {
                    Id = id,
                    AspNetUsersId = aspNetUserId,
                    Orders = new List<Order>()
                };
            }
            return customer;
        }

        public Order CreateOrder(string customerId, int cinemaHallId, int seanceId, IEnumerable<Position> requestedSeats)
        {
            Order order = null;
            Customer customer = GetCustomer(customerId);
            using (var conn = new SqlConnection(connectionStr))
            {
                var cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                var datatime = DateTime.Now;
                cmd.CommandText = @"insert into Orders
                    (OrderDate, CustomerId, SeanceId)
                    values
                    (@OrderDate, @CustomerId, @SeanceId);
                    select SCOPE_IDENTITY()";
                cmd.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                cmd.Parameters.AddWithValue("@SeanceId", seanceId);
                cmd.Connection.Open();
                int id = (int)cmd.ExecuteScalar();

                order = new Order
                {
                    Id = id,
                    CustomerId = customer.Id,
                    Customer = customer,
                    OrderDate = datatime,
                    SeanceId = seanceId
                };

                cmd.Parameters.Clear();
                cmd.CommandText = @"insert into Tickets
                        (AreaNumber, ColumnIndex, OrderId, RowIndex)
                        values
                        (@AreaNumber, @ColumnIndex, @OrderId, @RowIndex); 
                        select SCOPE_IDENTITY()";
                var areaNumber = new SqlParameter("@AreaNumber", SqlDbType.Int);
                var columnIndex = new SqlParameter("@ColumnIndex", SqlDbType.Int);
                var rowIndex = new SqlParameter("@RowIndex", SqlDbType.Int);
                var orderId = new SqlParameter("@OrderId", id);

                foreach (var seat in requestedSeats)
                {
                    areaNumber.Value = seat.AreaNumber;
                    columnIndex.Value = seat.ColumnIndex;
                    rowIndex.Value = seat.RowIndex;
                    int ticketId = (int)cmd.ExecuteScalar();
                    Ticket ticket = new Ticket
                    {
                        Id = ticketId,
                        AreaNumber = seat.AreaNumber,
                        ColumnIndex = seat.ColumnIndex,
                        RowIndex = seat.RowIndex,
                        OrderId = id,
                        Order = order
                    };
                    order.Tickets.Add(ticket);
                }
            }
            return order;
        }

        public CinemaHall GetCinemaHall(int cinema_id, int cinemahall_id)
        {
            CinemaHall cinemaHall = null;
            using (var conn = new SqlConnection(connectionStr))
            {
                var cmd = new SqlCommand();
                try
                {
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = @"select 
                        ch.Name as 'CinemaHallName' 
                        , c.Name as 'CinemaName'
						, ch.Schema_Url 
                        from CinemaHalls as ch
                        join Cinemas as c 
                        on ch.CinemaId = c.Id
                        where ch.Id = @Id and ch.CinemaId = @CinemaId";
                    var cinemaId = new SqlParameter("@CinemaId", cinema_id);
                    var cinemaHallId = new SqlParameter("@Id", cinemahall_id);
                    cmd.Parameters.Add(cinemaId);
                    cmd.Parameters.Add(cinemaHallId);
                    cmd.Connection.Open();
                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        cinemaHall = new CinemaHall
                        {
                            Id = cinemahall_id,
                            CinemaId = cinema_id,
                            Cinema = new Cinema
                            {
                                Id = cinema_id,
                                Name = (string)reader["CinemaName"],
                                Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? string.Empty : (string)reader["Address"]
                            },
                            Name = (string)reader["CinemaHallName"],
                            Schema_Url = (string)reader["Schema_Url"]
                        };
                    }
                }
                catch (Exception)
                {

                    throw;
                }
            }
            return cinemaHall;
        }

        public Customer GetCustomer(string aspnetuser_id)
        {
            Customer customer = null;
            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                var cmd = new SqlCommand();
                try
                {
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = @"select Id from Customers where AspNetUsersId = @AspNetUsersId";
                    cmd.Parameters.Add(new SqlParameter("AspNetUsersId", aspnetuser_id));

                    cmd.Connection.Open();
                    var id = cmd.ExecuteScalar();
                    if (id == null)
                        return customer;
                    customer = new Customer
                    {
                        Id = (int)id,
                        AspNetUsersId = aspnetuser_id
                    };
                }
                catch (Exception)
                {

                    throw;
                }
            }
            return customer;
        }

        public IEnumerable<Order> GetOrders(string aspnetuser_id)
        {
            Customer customer = GetCustomer(aspnetuser_id);
            if (customer == null)
                return null;
            Dictionary<int, Order> dictionary = new Dictionary<int, Order>();

            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                var cmd = new SqlCommand();
                try
                {
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = @"select
                        o.[Id]
                      , o.[OrderDate] 
                      , o.[CustomerId]
                      , o.[SeanceId]
	                  , s.CinemaHallId
	                  , s.[Date]
	                  , s.[Time]
                      , t.OrderId
                      , t.Id as 'TicketId'
                      , t.[AreaNumber]
                      , t.[ColumnIndex]
                      , t.[RowIndex]
                      , ch.[CinemaId]
                      , ch.[Name]
                      , ch.[Schema_Url]
                      , ci.[Name] as 'CinemaName'
                      , ci.[Address]
                      FROM Orders as o
                      join Seances as s
                        on o.SeanceId = s.Id
                      join CinemaHalls as ch
                        on s.CinemaHallId = ch.Id 
                      join Cinemas as ci
                        on ch.CinemaId = ci.Id
                      join Tickets as t
			            on o.Id = t.OrderId
				      where o.CustomerId = @CustomerId";

                    var param1 = new SqlParameter("@CustomerId", customer.Id);
                    cmd.Parameters.Add(param1);
                    cmd.Connection.Open();
                    var reader = cmd.ExecuteReader();


                    while (reader.Read())
                    {
                        Order order;
                        int orderId = (int)reader["Id"];
                        if (dictionary.ContainsKey(orderId))
                        {
                            order = dictionary[orderId];
                        }
                        else
                        {
                            order = new Order
                            {
                                Id = orderId,
                                CustomerId = customer.Id,
                                Customer = customer,
                                SeanceId = (int)reader["SeanceId"],
                                Seance = new Seance
                                {
                                    Id = (int)reader["SeanceId"],
                                    CinemaHallId = (int)reader["CinemaHallId"],
                                    Date = (DateTime)reader["Date"],
                                    Time = (string)reader["Time"]
                                },
                                OrderDate = (DateTime)reader["OrderDate"]
                            };
                            order.Seance.CinemaHall = new CinemaHall
                            {
                                Id = (int)reader["CinemaHallId"],
                                CinemaId = (int)reader["CinemaId"],

                                Name = (string)reader["Name"],
                                Schema_Url = (string)reader["Schema_Url"]
                            };
                            order.Seance.CinemaHall.Cinema = new Cinema
                            {
                                Id = (int)reader["CinemaId"],
                                Name = (string)reader["CinemaName"],
                                Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? string.Empty : (string)reader["Address"]
                            };
                            order.Tickets = new List<Ticket>();
                            dictionary.Add(order.Id, order);
                        }

                        Ticket ticket = new Ticket
                        {
                            Id = (int)reader["TicketId"],
                            OrderId = orderId,
                            AreaNumber = (int)reader["AreaNumber"],
                            ColumnIndex = (int)reader["ColumnIndex"],
                            RowIndex = (int)reader["RowIndex"],
                            Order = order
                        };
                        order.Tickets.Add(ticket);

                    }

                }
                catch (Exception)
                {

                    throw;
                }
            }
            List<Order> orders = new List<Order>(dictionary.Values);
            return orders;
        }

        public IEnumerable<Ticket> GetSeanceTickets(int value)
        {
            throw new NotImplementedException();
        }

        public Order RecompileOrders(string userId, int cinemaHallId, int seanceId, IEnumerable<Position> addSeats, IEnumerable<Position> removeSeats)
        {
            throw new NotImplementedException();
        }

        public void SaveData()
        {
            throw new NotImplementedException();
        }
    }
}
