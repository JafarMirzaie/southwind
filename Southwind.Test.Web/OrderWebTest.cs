﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Utilities;
using Signum.Web.Selenium;
using Southwind.Entities;
using Southwind.Test.Environment;

namespace Southwind.Test.Web
{
    [TestClass]
    public class OrderWebTest : Common
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            Common.Start(testContext);
            SouthwindEnvironment.StartAndInitialize();
            AuthLogic.GloballyEnabled = false;
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            Common.MyTestCleanup();
        }

        [TestMethod]
        public void OrderWebTestExample()
        {
            Login("Normal", "Normal");
            Lite<OrderEntity> lite = null;
            try
            {
                this.SearchPage(typeof(PersonEntity)).Using(persons =>
                {
                    persons.Search();
                    persons.SearchControl.Results.OrderBy("Id");
                    return persons.Results.EntityClick<PersonEntity>(1);
                }).Using(john =>
                {
                    using (PopupControl<OrderEntity> order = john.ConstructFromPopup(OrderOperation.CreateOrderFromCustomer))
                    {
                        order.ValueLineValue(a => a.ShipName, Guid.NewGuid().ToString());
                        order.EntityCombo(a => a.ShipVia).SelectLabel("FedEx");

                        ProductEntity sonicProduct = Database.Query<ProductEntity>().SingleEx(p => p.ProductName.Contains("Sonic"));

                        var line = order.EntityListDetail(a => a.Details).CreateElement<OrderDetailsEntity>();
                        line.EntityLineValue(a => a.Product, sonicProduct.ToLite());

                        Assert.AreEqual(sonicProduct.UnitPrice, order.ValueLineValue(a => a.TotalPrice));

                        order.ExecuteAjax(OrderOperation.SaveNew);

                        lite = order.GetLite();

                        Assert.AreEqual(sonicProduct.UnitPrice, order.ValueLineValue(a => a.TotalPrice));
                    }

                    return this.NormalPage(lite);

                }).EndUsing(order =>
                {
                    Assert.AreEqual(lite.InDB(a => a.TotalPrice), order.ValueLineValue(a => a.TotalPrice));
                });
            }
            finally
            {
                if (lite != null)
                    lite.Delete();
            }
        }//OrderWebTestExample
    }
}
