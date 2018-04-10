using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using CinemaBookingClient.Models;
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
            throw new NotImplementedException();
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
                da.InsertCommand.Parameters.Add(new SqlParameter("@AspNetUsersId", SqlDbType.NVarChar,450, "AspNetUsersId"));

                int newId = da.Update(ds.Tables[0]);
                newCustomer = new Customer { Id =(int) row["Id"], AspNetUsersId =(string) row["AspNetUsersId"] };
            }
            return newCustomer;
        }

        public Order CreateOrder(string userId, int cinemaHallId, int seanceId, IEnumerable<Position> requestedSeats)
        {
            throw new NotImplementedException();
        }

        public CinemaHall GetCinemaHall(int cinema_id, int cinemahall_id)
        {
            throw new NotImplementedException();
        }

        public Customer GetCustomer(string aspnetuser_id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Order> GetOrders(string aspnetuser_id)
        {
            throw new NotImplementedException();
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
