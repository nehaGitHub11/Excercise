using NehaExerciseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NehaExercise
{
    public static class TrolleyCalculator
    {
        public static decimal CalculateTrolleyTotal(Trolley trolley)
        {
            decimal trolleyTotalPrice = 0;
            //getallproducts()->loop all products ==>
            //find if this product present in quantities,
            //if yes getQuantity, check if product and quantity meeting specials if yes getspecialprice
            //else calculate price *quantity
            var specials = trolley.specials;

            var productWithQuantities = (from p in trolley.products
                                         join q in trolley.quantities
                                         on p.Name equals q.name
                                         select new Product { Name = p.Name, Price = p.Price, Quantity = q.quantity }).ToList();
            
            foreach (var item in productWithQuantities)
            {
                if (specials.Count >= 1)
                {
                    trolleyTotalPrice = 1000;
                }
                else
                {
                    trolleyTotalPrice += item.Price * item.Quantity;
                }
            }
            return trolleyTotalPrice;
        }
    }
}
