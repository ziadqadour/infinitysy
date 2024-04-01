﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Nop.Services.Events;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.UI;

namespace NopStation.Plugin.Misc.Core.Infrastructure
{
    public class EventConsumers : IConsumer<PageRenderingEvent>
    {
        private readonly IActionContextAccessor _actionContextAccessor;

        public EventConsumers(IActionContextAccessor actionContextAccessor)
        {
            _actionContextAccessor = actionContextAccessor;
        }

        public Task HandleEventAsync(PageRenderingEvent eventMessage)
        {
            var area = _actionContextAccessor.ActionContext.HttpContext.GetRouteValue("area");
            if (area != null && area.ToString().Equals("admin", StringComparison.InvariantCultureIgnoreCase))
            {
                eventMessage.Helper.AppendCssFileParts("~/Plugins/NopStation.Core/contents/lib/select2/select2.min.css", "");
                eventMessage.Helper.AppendCssFileParts("~/Plugins/NopStation.Core/contents/css/style.css", "");

                eventMessage.Helper.AppendScriptParts(ResourceLocation.Footer, "~/Plugins/NopStation.Core/contents/lib/select2/select2.min.js");
            }

            return Task.CompletedTask;
        }
    }
}