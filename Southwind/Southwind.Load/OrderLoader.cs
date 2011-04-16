﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine;
using Southwind.Entities;
using Signum.Utilities;
using Signum.Entities;
using Signum.Services;
using System.Globalization;

namespace Southwind.Load
{
    internal static class OrderLoader
    {
        public static void LoadShippers()
        {
            using (NorthwindDataContext db = new NorthwindDataContext())
            {
                Administrator.SaveListDisableIdentity(db.Shippers.Select(s =>
                    Administrator.SetId(s.ShipperID, new ShipperDN
                    {
                        CompanyName = s.CompanyName,
                        Phone = s.Phone,
                    })));
            }
        }

        public static void LoadOrders()
        {
            using (NorthwindDataContext db = new NorthwindDataContext())
            {
                var northwind = db.Customers.Select(a => new { a.CustomerID, a.ContactName }).ToList();

                var companies = Database.Query<CompanyDN>().Select(c => new 
                { 
                    Lite = c.ToLite<CustomerDN>(), 
                    c.ContactName 
                }).ToList();

                var persons = Database.Query<PersonDN>().Select(p => new 
                { 
                    Lite = p.ToLite<CustomerDN>(), 
                    ContactName = p.FirstName + " " + p.LastName 
                }).ToList();

                Dictionary<string, Lite<CustomerDN>> customerMapping = 
                    (from n in northwind
                     join s in companies.Concat(persons) on n.ContactName equals s.ContactName
                     select new KeyValuePair<string, Lite<CustomerDN>>(n.CustomerID, s.Lite)).ToDictionary();

                using(Transaction tr = new Transaction())
                using (Administrator.DisableIdentity<OrderDN>())
                {
                    IProgressInfo info;
                    foreach (Order o in db.Orders.ToProgressEnumerator(out info))
                    {
                        Administrator.SetId(o.OrderID, new OrderDN
                        {
                            Employee = new Lite<EmployeeDN>(o.EmployeeID.Value),
                            OrderDate = o.OrderDate.Value,
                            RequiredDate = o.RequiredDate.Value,
                            ShippedDate = o.ShippedDate,
                            ShipVia = new Lite<ShipperDN>(o.ShipVia.Value),
                            ShipName = o.ShipName,
                            ShipAddress = new AddressDN
                            {
                                Address = o.ShipAddress,
                                City = o.ShipCity,
                                Region = o.ShipRegion,
                                PostalCode = o.ShipPostalCode,
                                Country = o.ShipCountry,
                            },
                            Freight = o.Freight.Value,
                            Details = o.Order_Details.Select(od => new OrderDetailsDN
                            {
                                Discount = (decimal)od.Discount,
                                Product = new Lite<ProductDN>(od.ProductID),
                                Quantity = od.Quantity,
                                UnitPrice = od.UnitPrice,
                            }).ToMList(),
                            Customer = customerMapping[o.CustomerID].RetrieveAndForget(),
                            IsLegacy = true,
                        }).Save();

                        SafeConsole.WriteSameLine(info.ToString());
                    }

                    tr.Commit();
                }
            }
        }
    }
}