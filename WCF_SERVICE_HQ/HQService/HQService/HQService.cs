﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace HQService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in both code and config file together.
    public class HQService : IHQService
    {
        public int acceptStockRequest(int orderId)
        {
            using (var ctx = new ChocolateCoHQEntities1())
            {
                UkBranchServiceReference.ProductServiceClient client = 
                                new UkBranchServiceReference.ProductServiceClient();
                PendingStockOrders pso = ctx.PendingStockOrders.Find(orderId);
                bool ret = client.deliverStock(pso.ProductID, pso.QuantityAsked);
                if (ret)
                {
                    StockOrdersLog sol = new StockOrdersLog();
                    sol.OrderId = orderId;
                    sol.ProductID = pso.ProductID;
                    sol.branch = pso.branch;
                    sol.Quantity = pso.QuantityAsked;
                    ctx.StockOrdersLog.Add(sol);
                    ctx.PendingStockOrders.Remove(pso);
                    int rowcount = ctx.SaveChanges();
                    return rowcount;
                } else
                {
                    return -1;
                }
                client.Close();
            }
            
        }

        public bool logLocalOrder(int orderId, int localClientId, int productId, string date, int quantity, int localShipperId, bool isAccepted)
        {
            using (var ctx = new ChocolateCoHQEntities1())
            {
                OrdersLog ol = new OrdersLog();
                ol.OrderID = orderId;
                ol.LocalClientID = localClientId;
                ol.ProductID = productId;
                ol.Date = DateTime.Parse(date);
                ol.Quantity = quantity;
                ol.LocalShipperID = localShipperId;
                ol.Accepted = (short) (isAccepted ? 1 : 0);
                ctx.OrdersLog.Add(ol);
                int ret = ctx.SaveChanges();
                return (ret > 0);
            }
        }

        public int requestStockHQ(int proposedOrderId, string branch, int productId, int quantityAsked)
        {
            using (var ctx = new ChocolateCoHQEntities1())
            {
                PendingStockOrders pso = new PendingStockOrders();
                pso.branch = branch;
                pso.ProductID = productId;
                pso.QuantityAsked = quantityAsked;
                int maxLoggedId = ctx.OrdersLog.Max(o => o.OrderID);
                int maxPendingId = ctx.PendingStockOrders.Max(o => o.OrderID);
                int finalId = Math.Max(maxLoggedId, maxPendingId);
                finalId = Math.Max(finalId, proposedOrderId);
                pso.OrderID = finalId;
                ctx.PendingStockOrders.Add(pso);
                int ret = ctx.SaveChanges();
                if (ret > 0) return finalId;
                else return -1;
            }
        }

        
    }
}
