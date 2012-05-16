﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Entities;
using Signum.Engine;
using Signum.Web;
using Southwind.Entities;
using Southwind.Logic;
using Southwind.Web;
using Signum.Entities.Authorization;
using Signum.Engine.Authorization;
using Signum.Engine.ControlPanel;
using Signum.Web.ControlPanel;

namespace Southwind.Web.Controllers
{
    [HandleError]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var panel = ControlPanelLogic.GetHomePageControlPanel();
            if (panel != null)
                return View(ControlPanelClient.ViewPrefix.Formato("ControlPanel"), panel.Retrieve());
            else
                return View();
        }

        public ActionResult ChangeTheme()
        {
            Session[SouthwindClient.ThemeSessionKey] = Request.Params["themeSelector"];
            return Redirect(Request.UrlReferrer.AbsolutePath);
        }

        public FileResult EmployeePhoto(Lite<EmployeeDN> employee)
        {
            return File(employee.InDB().Select(e => e.Photo).Single(), "image/jpeg");
        }

        public FileResult CategoryPhoto(Lite<CategoryDN> employee)
        {
            return File(employee.InDB().Select(e => e.Picture).Single(), "image/jpeg");
        }

        public ContentResult UpdateOrders()
        {
            int removed = Database.Query<OrderDN>().Where(a => a.Id > 11077).UnsafeDelete(); 

            DateTime time = Database.Query<OrderDN>().Max(a => a.OrderDate);

            var now = TimeZoneManager.Now;
            var ts = (int)(now - time).TotalDays;

            int updated = Database.Query<OrderDN>().UnsafeUpdate(o => new OrderDN
            {
                OrderDate = o.OrderDate.AddDays(ts),
                ShippedDate = o.ShippedDate.Value.AddDays(ts),
                RequiredDate = o.RequiredDate.AddDays(ts),
                CancelationDate = null,
            });

            return Content("Removed: {0}\r\nUpdated: {1}".Formato(removed, updated)); 
        }
    }
}