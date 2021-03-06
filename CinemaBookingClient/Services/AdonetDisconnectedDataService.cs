﻿using System;
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
    public class AdonetDisconnectedDataService : ICinemaDataService
    {
        private IConfiguration configuration;
        string connectionStr;

        public AdonetDisconnectedDataService(IConfiguration configuration)
        {
            this.configuration = configuration;
            connectionStr = configuration.GetConnectionString("DefaultConnection");
        }

        public int CancelTickets(string userId, int cinemaHallId, int seanceId, IEnumerable<Position> removeSeats)
        {
            int cnt = 0;
            if (removeSeats == null)
                return 0;
            var seats = removeSeats.Where(z => z != null);
            if (seats.Count() == 0)
                return 0;
            HashSet<Position> toRemove = new HashSet<Position>(seats);

            using (var conn = new SqlConnection(connectionStr))
            {
                string sql = "select * from Tickets";
                var da = new SqlDataAdapter(sql, conn);
                var ds = new DataSet();
                da.Fill(ds, "Tickets");
                DataTable dt = ds.Tables["Tickets"];
                foreach (var pos in toRemove)
                {
                    object[] findVals = new object[3];
                    findVals[0] = pos.AreaNumber;
                    findVals[1] = pos.ColumnIndex;
                    findVals[2] = pos.RowIndex;

                    DataRow row = dt.Rows.Find(findVals);
                    int index = dt.Rows.IndexOf(row);
                    string delSql = "Delete from Tickets where Id = @Id";
                    da.DeleteCommand = new SqlCommand(delSql, conn);
                    da.DeleteCommand.Parameters.Add(new SqlParameter("@Id", index));
                    da.Update(ds.Tables["Tickets"]);
                    cnt++;
                }
            }
            return cnt;
        }

        public Customer CreateCustomer(string aspNetUserid)
        {
            //https://docs.microsoft.com/ru-ru/dotnet/framework/data/adonet/updating-data-sources-with-dataadapters
            Customer newCustomer = null;
            using (var conn = new SqlConnection(connectionStr))
            {
                string sql = "select * from Customers";
                var da = new SqlDataAdapter(sql, conn);
                var ds = new DataSet();
                da.Fill(ds, "Customers");
                ds.Tables[0].Constraints.Add("PK_Customers", ds.Tables[0].Columns[0], true);
                DataRow row;
                row = ds.Tables[0].NewRow();
                row["Id"] = -1;
                row["AspNetUsersId"] = aspNetUserid;
                ds.Tables[0].Rows.Add(row);
                string insertSql = "INSERT INTO[dbo].[Customers] ([AspNetUsersId]) VALUES(@AspNetUsersId); SELECT Id, AspNetUsersId FROM Customers WHERE(Id = SCOPE_IDENTITY())";
                da.InsertCommand = new SqlCommand(insertSql, conn);
                da.InsertCommand.Parameters.Add(new SqlParameter("@AspNetUsersId", SqlDbType.NVarChar, 450, "AspNetUsersId"));

                int newId = da.Update(ds.Tables[0]);
                newCustomer = new Customer { Id = (int)row["Id"], AspNetUsersId = (string)row["AspNetUsersId"] };
            }
            return newCustomer;
        }

        public Order CreateOrder(string userId, int cinemaHallId, int seanceId, IEnumerable<Position> requestedSeats)
        {
            throw new NotImplementedException();
        }

        public CinemaHall GetCinemaHall(int cinema_id, int cinemahall_id)
        {
            CinemaHall cinemaHall = null;
            using (var conn = new SqlConnection(connectionStr))
            {
                string sql = @"select ch.Name as 'CinemaHallName' 
                        , c.Name as 'CinemaName'
						, ch.Schema_Url 
                        from CinemaHalls as ch
                        join Cinemas as c 
                        on ch.CinemaId = c.Id
                        where ch.Id = @Id and ch.CinemaId = @CinemaId";

                var da = new SqlDataAdapter(sql, conn)
                {
                    SelectCommand = new SqlCommand(sql, conn)
                };
                da.SelectCommand.Parameters.Add(new SqlParameter("@Id", cinemahall_id));
                da.SelectCommand.Parameters.Add(new SqlParameter("@CinemaId", cinema_id));
                var ds = new DataSet();
                da.Fill(ds, "FoundCinemaHall");
                DataRow foundCinemaHall = ds.Tables[0].Rows[0];

                cinemaHall = new CinemaHall
                {
                    Id = cinemahall_id,
                    Name = (string)foundCinemaHall["CinemaHallName"],
                    CinemaId = cinema_id,
                    Cinema = new Cinema
                    {
                        Id = cinema_id,
                        Name = (string)foundCinemaHall["CinemaName"]
                    },
                    Schema_Url = (string)foundCinemaHall["Schema_Url"]
                };
            }
            return cinemaHall;
        }

        public Customer GetCustomer(string aspnetuser_id)
        {
            Customer foundCustomer = null;
            using (var conn = new SqlConnection(connectionStr))
            {
                string sql = "select * from Customers";
                SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                DataSet ds = new DataSet();
                da.Fill(ds, "Customers");
                ds.Tables[0].Constraints.Add("PK_Customers", ds.Tables[0].Columns[0], true);
                var row = ds.Tables[0].Select("AspNetUsersId =\'" + aspnetuser_id + "\'")[0];
                if (row != null)
                {
                    foundCustomer = new Customer { Id = (int)row["Id"], AspNetUsersId = (string)row["AspNetUsersId"] };
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            return foundCustomer;
        }

        public IEnumerable<Order> GetOrders(string aspnetuser_id)
        {
            Customer customer = GetCustomer(aspnetuser_id);
            if (customer == null)
                return null;
                        
            DataTable dt = GetOrdersDB(customer.Id);
            if(dt.Rows.Count < 0)
            {
                return null;
            }

            List<Order> orders  = new List<Order>();
            var group = dt.Rows.Cast<DataRow>()
                .GroupBy(row => row["Id"])
                .ToList()
                ;

            foreach (var el in group)
            {
                var row = el.FirstOrDefault();

                Order order = new Order
                {
                    Id = (int)row["Id"],
                    CustomerId = (int)row["CustomerId"],
                    Customer = new Customer
                    {
                        Id = (int)row["CustomerId"],
                        AspNetUsersId = customer.AspNetUsersId
                        //TODO orders?
                    },
                    SeanceId = (int)row["SeanceId"],
                    Seance = new Seance { },
                    OrderDate = DateTime.Parse((string)row["OrderDate"])
                };

                var tickets = el.Select(z => new Ticket
                {
                    Id = (int)z["TicketId"],
                    AreaNumber = (int) z["AreaNumber"],
                    ColumnIndex = (int)z["ColumnIndex"],
                    RowIndex = (int)z["RowIndex"],
                    OrderId = (int)z["Id"]                    
                });

                order.Tickets = tickets.ToList();
                orders.Add(order);
            }

            return orders;
        }

        private DataTable GetOrdersDB(int customerId)
        {
            DataTable dt = null;
            using (var conn = new SqlConnection(connectionStr))
            {
                string sql = @"select 
                    o.[OrderDate] 
                  , o.[CustomerId]
                  , o.[SeanceId]
	              , s.CinemaHallId
	              , s.[Date]
	              , s.[Time]
                  , t.OrderId
                  , t.Id as 'TicketId'
                  FROM Orders as o
                  join Seances as s
                    on o.SeanceId = s.Id
                  join CinemaHalls as ch
                    on s.CinemaHallId = ch.Id 
                  join Tickets as t
			        on o.Id = t.OrderId
				  where o.CustomerId = @CustomerId";

                var da = new SqlDataAdapter(sql, conn)
                {
                    SelectCommand = new SqlCommand(sql, conn)
                };
                da.SelectCommand.Parameters.Add(new SqlParameter("@CustomerId", customerId));
                DataSet ds = new DataSet();
                da.Fill(ds, "Orders");
                dt = ds.Tables["Orders"];
            }
            return dt;
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
