using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace AuroraPOS.Models
{
    public class Order : BaseEntity
    {
        public OrderType OrderType { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Propina { get; set; }
        public decimal Tip { get; set; }
        public decimal Discount { get; set; }
        public decimal? Delivery { get; set; }
        public virtual Station Station { get; set; }
        public virtual Area? Area { get; set; }
        public virtual AreaObject? Table { get; set; }
        public decimal Balance { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public decimal PayAmount { get; set; }
        public string? ClientName { get; set; }
        public long CustomerId { get; set; }
        public long? ConduceOrderId { get; set; }
        public string? WaiterName { get; set; }
        public bool IsDivider { get; set; }
        public string? Note { get; set; }
        public string? Terms { get; set; }
        public int Person { get; set; }
        public DateTime OrderTime { get; set; }
        public OrderMode OrderMode { get; set; }
        public List<OrderItem>? Items { get; set; }
        public List<DiscountItem>? Discounts { get; set; }
        public List<TaxItem>? Taxes { get; set; }
        public List<PropinaItem>? Propinas { get; set; }
        public List<SeatItem>? Seats { get; set; }
        public List<DividerItem>? Divides { get; set; }
        public List<OrderTransaction>? Transactions { get; set; }
        public long ComprobantesID { get; set; } = 0;
        public string? ComprobanteNumber { get; set; }
        public string? ComprobanteName { get; set; }
        public long? PrepareTypeID { get; set; }
        public virtual PrepareTypes? PrepareType { get; set; }
        public decimal PromesaPago { get; set; }
        public long? Factura { get; set; }
        public bool IsConduce { get; set; }
        public decimal GetTotalPrice(Voucher voucher, int DivideNum = 0, int SeatNum = 0)
        {
            decimal total = 0;
            decimal alltotal = 0;

            var taxes = new List<TaxItem>();
            var propinas = new List<PropinaItem>();
            decimal paidtotal = 0;
            DiscountItem orderDiscount = null;
            if (this.Discounts != null && this.Discounts.Count == 1)
            {
                orderDiscount = this.Discounts[0];
            }
            if (Items != null)
            {
                foreach (var item in Items)
                {
                    if (item.IsDeleted) continue;
                    if (DivideNum > 0 && DivideNum != item.DividerNum) continue;
                    if (SeatNum > 0 && SeatNum != item.SeatNum) continue;
                    
                    if (OrderType == OrderType.Barcode)
                    {
                        foreach (var t in item.Taxes)
                        {
                            t.IsExempt = true;
                        }

                        var tax = item.Taxes.FirstOrDefault(s => !s.BarcodeExclude);
                        tax.IsExempt = false;
                        var subtotal = item.Qty * item.Price;
                        if (tax != null)
                        {
                            subtotal = subtotal * 100 / (100 + tax.Percent);
                        }

                        decimal discount = 0.0m;
                        if (item.Discounts != null && item.Discounts.Count > 0)
                        {
                            decimal discountall = 0;
                            foreach (var d in item.Discounts)
                            {
                                var amount = d.Amount;
                                if (d.AmountType == AmountType.Percent)
                                {
                                    amount = subtotal * d.BaseAmount / 100.0m;

                                }
                                d.Amount = amount;
                                discountall += d.Amount;
                            }
                            discount = discountall;
                        }

                        decimal subvalue = subtotal - discount;

                        decimal taxAmount = 0;
                        if (tax != null)
                        {
                            taxAmount = subvalue * tax.Percent / 100;
                        }

                        item.Tax = taxAmount;
                        var exist = taxes.FirstOrDefault(s => s.TaxId == tax.TaxId);
                        if (exist != null)
                        {
                            exist.Amount += taxAmount;
                        }
                        else
                        {
                            exist = new TaxItem();
                            exist.TaxId = tax.TaxId;

                            exist.Description = tax.Description;
                            exist.Amount = taxAmount;
                            exist.Percent = tax.Percent;
                            taxes.Add(exist);
                        }

                        item.SubTotal = subtotal - discount;
                        total += subtotal;
                    }
                    else
                    {
                        decimal subtotal = 0;
                        decimal discount = 0;
                        (subtotal, discount) = item.GetSubTotal();
                        item.Tax = 0;

						if (orderDiscount != null)
						{
							orderDiscount.Amount = orderDiscount.BaseAmount;
							if (orderDiscount.AmountType == AmountType.Percent)
								orderDiscount.Amount = subtotal * orderDiscount.BaseAmount / 100;

							discount = orderDiscount.Amount;
						}

						if (item.Taxes != null)
                        {
                            foreach (var t in item.Taxes)
                            {
                                bool exempt = false;
                                var value = (subtotal - discount) * t.Percent / 100.0m;
                                t.Amount = value;
                                if (t.IsExempt) continue;
                                if (this.OrderType == OrderType.Delivery)
                                {

                                    if (t.ToGoExclude)
                                    {
                                        t.IsExempt = true;
                                        continue;
                                    }
                                }

                                if (voucher != null)
                                {
                                    var voucherTax = voucher.Taxes.FirstOrDefault(s => s.ID == t.TaxId);
                                    if (voucherTax != null)
                                    {
                                        t.IsExempt = true;
                                        continue;
                                    }
                                }

                                item.Tax += value;
                                var exist = taxes.FirstOrDefault(s => s.TaxId == t.TaxId);
                                if (exist != null)
                                {
                                    exist.Amount += value;
                                }
                                else
                                {
                                    exist = new TaxItem();
                                    exist.TaxId = t.TaxId;

                                    exist.Description = t.Description;
                                    exist.Amount = value;
                                    exist.Percent = t.Percent;
                                    taxes.Add(exist);
                                }
                            }
                        }

                        item.Propina = 0;
                        if (item.Propinas != null)
                        {
                            foreach (var t in item.Propinas)
                            {
                                bool exempt = false;
                                var value = (subtotal - discount) * t.Percent / 100.0m;
                                t.Amount = value;
                                if (t.IsExempt) continue;
                                if (this.OrderType == OrderType.Delivery)
                                {

                                    if (t.ToGoExclude)
                                    {
                                        t.IsExempt = true;
                                        continue;
                                    }
                                }
                                                               

                                item.Propina += value;
                                var exist = propinas.FirstOrDefault(s => s.PropinaId == t.PropinaId);
                                if (exist != null)
                                {
                                    exist.Amount += value;
                                }
                                else
                                {
                                    exist = new PropinaItem();
                                    exist.PropinaId = t.PropinaId;

                                    exist.Description = t.Description;
                                    exist.Amount = value;
                                    exist.Percent = t.Percent;
                                    propinas.Add(exist);
                                }
                            }
                        }

                        item.SubTotal = subtotal;
                        total += subtotal;
                    }

                }
            }
           
            SubTotal = total;
            
            if (orderDiscount != null)
            {
				orderDiscount.Amount = orderDiscount.BaseAmount;
                if (orderDiscount.AmountType == AmountType.Percent)
					orderDiscount.Amount = total * orderDiscount.BaseAmount / 100;

                Discount = orderDiscount.Amount;
			}
            total = total - Discount;
            

			if (this.Taxes != null)
				this.Taxes.Clear();
			//foreach (var t in taxes)
			//{
   //             t.Amount = total * t.Percent / 100.0m;
			//}
			this.Taxes = taxes;
			this.Tax = taxes.Sum(s => s.Amount);

			if (this.Propinas != null)
				this.Propinas.Clear();

   //         foreach(var p in propinas)
   //         {
   //             p.Amount = total * p.Percent / 100.0m;
			//}
			this.Propinas = propinas;
			this.Propina = propinas.Sum(s => s.Amount);

			{
				TotalPrice = total + this.Tax + this.Propina + (Delivery ?? 0);
			}

			if (DivideNum > 0)
            {
                Balance = TotalPrice;
            }
            else if (SeatNum > 0)
            {
                Balance = TotalPrice;
            }
            else
            {
                Balance = TotalPrice - PayAmount;
            }


            return total;
        }

        public bool CheckPromotion()
        {
            foreach (var item in Items)
            {
                if (item.Status == OrderItemStatus.Paid) continue;

                var promotion = item.Discounts.Where(s => s.ItemType == DiscountItemType.Promotion).ToList();
                if (promotion.Any())
                {
                    return true;
                }
            }
            return false;
        }

        public void AddDiscount(Discount discount, int divideId = 0)
        {
            if (this.Discounts == null) Discounts = new List<DiscountItem>();
            var amount = discount.Amount;

            Discounts.Add(new DiscountItem()
            {
                Name = discount.Name,
                Amount = amount,
                BaseAmount = amount,
                AmountType = discount.DiscountAmountType,
                ItemType = DiscountItemType.Discount,
                TargetType = DiscountTargetType.Order,
                ItemID = discount.ID,
                DividerId = divideId
            });
        }
    }

    public class CanceledOrderItem : BaseEntity
    {
        public CancelReason Reason { get; set; }
        public OrderItem Item { get; set; }
        public Product Product { get; set; }
    }

    public class CancelReason : BaseEntity
    {
        public string Reason { get; set; }
        public int Level { get; set; }
        public bool IsPrintAccount { get; set; }
        public bool IsPrintOverrideChannel { get; set; }
        public bool IsReduceInventory { get; set; }

        public override string ToString()
        {
            return Reason;
        }
    }
    public class OrderItem : BaseEntity, ICloneable
    {
        public long OrderID { get; set; }
        public Order Order { get; set; }
        public Product Product { get; set; }
        public long MenuProductID { get; set; }
        public string Name { get; set; }
        public int SeatNum { get; set; }
        public OrderItemStatus Status { get; set; }
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal Qty { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal Propina { get; set; }
        public DateTime HoldTime { get; set; }
        public int DividerNum { get; set; }
        public bool HasPromotion { get; set; } = false;
        public string? Note { get; set; }
        public string? Course { get; set; }
        public long CourseID { get; set; }
        public int ServingSizeID { get; set; }
        public string? ServingSizeName { get; set; }
        public decimal Costo { get; set; }
        public decimal AnswerCosto { get; set; }
        public decimal AnswerVenta { get; set; }
        public List<QuestionItem>? Questions { get; set; }
        public List<DiscountItem>? Discounts { get; set; }
        public List<TaxItem>? Taxes { get; set; }
        public List<PropinaItem>? Propinas { get; set; }
        public object Clone()
        {
            return this.MemberwiseClone();
        }
        public OrderItem CopyThis()
        {
            var nItem = new OrderItem();
            nItem.Order = this.Order;
            nItem.Product = this.Product;
            nItem.MenuProductID = this.MenuProductID;
            nItem.ServingSizeID = this.ServingSizeID;
            nItem.Name = this.Name;
            nItem.Status = this.Status;
            nItem.Price = this.Price;
            nItem.OriginalPrice = this.OriginalPrice;
            nItem.Qty = this.Qty;
            nItem.SubTotal = this.SubTotal;
            nItem.Discount = this.Discount;
            nItem.HoldTime = this.HoldTime;
            nItem.DividerNum = this.DividerNum;
            nItem.HasPromotion = this.HasPromotion;
            nItem.Course = this.Course;
            nItem.CourseID = this.CourseID;
            nItem.ServingSizeName = this.ServingSizeName;
            nItem.Note = this.Note;
            nItem.Questions = new List<QuestionItem>();
            if (this.Questions != null && this.Questions.Any())
            {
                nItem.Questions = new List<QuestionItem>();
                foreach (var q in this.Questions)
                {
                    var nq = new QuestionItem()
                    {
                        Description = q.Description,
                        Price = q.Price,
                        Answer = q.Answer,
                        ServingSizeID = q.ServingSizeID,
                        ServingSizeName = q.ServingSizeName,
                        CanRoll = q.CanRoll,
                        Qty = q.Qty,
                        Divisor = q.Divisor,
                        SubDescription = q.SubDescription,
                        IsPreSelect = q.IsPreSelect,
                        FreeChoice = q.FreeChoice,
                        IsActive = q.IsActive,
                    };
                    nItem.Questions.Add(nq);
                }
            }
            if (this.Discounts != null && this.Discounts.Any())
            {
                nItem.Discounts = new List<DiscountItem>();
                foreach (var d in this.Discounts)
                {
                    var nq = new DiscountItem()
                    {
                        Amount = d.Amount,
                        BaseAmount = d.BaseAmount,
                        Name = d.Name,
                        ItemID = d.ItemID,
                        ItemType = d.ItemType,
                        TargetType = d.TargetType,
                    };
                    nItem.Discounts.Add(nq);
                }
            }
            if (this.Taxes != null && this.Taxes.Any())
            {
                nItem.Taxes = new List<TaxItem>();
                foreach (var d in this.Taxes)
                {
                    var nq = new TaxItem()
                    {
                        Amount = d.Amount,
                        Description = d.Description,
                        TaxId = d.TaxId,
                        Percent = d.Percent,
                        ToGoExclude = d.ToGoExclude,
                        BarcodeExclude = d.BarcodeExclude,
                    };
                    nItem.Taxes.Add(nq);
                }
            }
            if (this.Propinas != null && this.Propinas.Any())
            {
                nItem.Propinas = new List<PropinaItem>();
                foreach (var d in this.Propinas)
                {
                    var nq = new PropinaItem()
                    {
                        Amount = d.Amount,
                        Description = d.Description,
                        PropinaId = d.PropinaId,
                        Percent = d.Percent,
                        ToGoExclude = d.ToGoExclude,
                        BarcodeExclude = d.BarcodeExclude,
                    };
                    nItem.Propinas.Add(nq);
                }
            }
            return nItem;
        }
        public (decimal, decimal) GetSubTotal()
        {
            if (this.Questions != null)
            {
                var rollpriceQuestions = this.Questions.Where(s => s.CanRoll);
                foreach (var q in rollpriceQuestions)
                {
                    var qty = q.Qty - q.FreeChoice;
                    this.Price = this.OriginalPrice + q.Price * qty;
                }
            }

            decimal total = this.Price * this.Qty;
			decimal discountall = 0;
			
            {
                if (this.Discounts != null)
                {
                    
                    foreach (var d in this.Discounts)
                    {
                        var amount = d.Amount;

                        if (d.ItemType != DiscountItemType.Promotion)
                        {
                            if (d.AmountType == AmountType.Percent)
                            {
                                amount = total * d.BaseAmount / 100.0m;
                            }
                        }
                       
                        d.Amount = amount;
                        discountall += d.Amount;
                    }
                    total -= discountall;
                }
            }

            if (this.Questions != null)
            {
                var nonrollpriceQuestions = this.Questions.Where(s => !s.CanRoll);
               
                foreach (var m in nonrollpriceQuestions)
                {
                    if (!m.IsActive) continue;
                    var qty = m.Qty - m.FreeChoice;

                    total += m.Price * qty * this.Qty;
                }
            }

            return (total, 0);
        }

        public void AddDiscount(Discount discount)
        {
            if (this.Discounts == null) Discounts = new List<DiscountItem>();
            var amount = discount.Amount;
            if (discount.DiscountAmountType == AmountType.Percent)
            {
                amount = (decimal)Price * (decimal)discount.Amount / 100.0m;
            }

            Discounts.Add(new DiscountItem()
            {
                Name = discount.Name,
                Amount = amount,
                BaseAmount = discount.Amount,
                ItemType = DiscountItemType.Discount,
                TargetType = DiscountTargetType.OrderItem,
                AmountType = discount.DiscountAmountType,
                ItemID = discount.ID
            });
        }

        public void AddPromotion(Promotion promotion, decimal qty = 0)
        {
            Discounts = new List<DiscountItem>();

            var amount = promotion.Amount;
            decimal applyqty = (int)qty;
            //if (promotion.ApplyType == PromotionApplyType.FirstCount)
            //{
            //    applyqty = qty - promotion.FirstCount;
            //    if (applyqty > Qty)
            //    {
            //        applyqty = Qty;
            //    }
            //}
            //else
            //{
            //    applyqty = (int)(qty);
            //    //applyqty = applyqty * promotion.FirstCount;
            //}

            if (promotion.AmountType == AmountType.Percent)
            {
                amount = applyqty * Price * promotion.Amount / 100.0m;
            }

            var exist = Discounts.FirstOrDefault(s => s.ItemType == DiscountItemType.Promotion && s.ItemID == promotion.ID);
            if (exist != null)
            {
                exist.Amount = amount;
            }
            else
            {
                HasPromotion = true;
                Discounts.Add(new DiscountItem()
                {
                    Name = promotion.Name,
                    ItemID = promotion.ID,
                    BaseAmount = promotion.Amount,
                    AmountType = promotion.AmountType,
                    TargetType = DiscountTargetType.OrderItem,
                    ItemType = DiscountItemType.Promotion,
                    Amount = amount,
                });
            }
        }

        
    }

    public class BarcodeShowOrderItem : OrderItem
    {
        public decimal StockQty { get; set; }
        public string Unit { get; set; }
        public List<ItemUnit> Units { get; set; }

        public BarcodeShowOrderItem(OrderItem item)
        {
            ID = item.ID;
            Order = item.Order;
            Product = item.Product;
            Name = item.Name;
            MenuProductID = item.MenuProductID;
            Status = item.Status;
            Price = item.Price;
            Qty = item.Qty;
            SubTotal = item.SubTotal;
            Discount = item.Discount;
            Tax = item.Tax;
        }

    }

    public class TaxItem : BaseEntity
    {
        public long TaxId { get; set; }
        public string Description { get; set; }
        public decimal Percent { get; set; }
        public decimal Amount { get; set; }
        public bool IsExempt { get; set; } = false;
        public bool ToGoExclude { get; set; }
        public bool BarcodeExclude { get; set; }
    }

    public class QuestionItem : BaseEntity, ICloneable
    {
        public Answer Answer { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public bool CanRoll { get; set; }
        public int ServingSizeID { get; set; }
        public string? ServingSizeName { get; set; }
        public decimal Qty { get; set; }
        public DivisorType Divisor { get; set; }
        public string? SubDescription { get; set; }
        public decimal SubPrice { get; set; }
        public bool IsPreSelect { get; set; }
        public int FreeChoice { get; set; }
        public long OrderItemID { get; set; }
        [NotMapped]
        public bool showDesc { get; set; }
        
        

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    public class DiscountItem : BaseEntity, ICloneable
    {
        public string Name { get; set; }
        public DiscountTargetType TargetType { get; set; }
        public DiscountItemType ItemType { get; set; }
        public AmountType AmountType { get; set; }
        public long ItemID { get; set; }
        public decimal Amount { get; set; }
        public decimal BaseAmount { get; set; }
        public int DividerId { get; set; }
        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    public enum DiscountItemType
    {
        Discount,
        Promotion
    }

    public enum DiscountTargetType
    {
        OrderItem,
        Order
    }

    public enum OrderMode
    {
        Standard,
        Seat,
        Divide,
        Course,
        Invoice,
        Quote,
        Conduce
    }

    public enum OrderType
    {
        FastExpress,
        Delivery,
        TakeAway,
        DiningRoom,
        Barcode,
        Kiosk,
    }

    public enum OrderStatus
    {
        Pending,
        Hold,
        Void,
        Paid,
        Fulfilled,
        Temp,
        Moved,
        Saved
    }
    public enum OrderItemStatus
    {
        Pending,
        Kitchen,
        HoldAutomatic,
        HoldManually,
        Paid,
        Saved,
        Printed
    }

    public enum OrderPaymentStatus
    {
        Unpaid,
        PartlyPaid,
        SeatPaid,
        DividerPaid,
        Paid,
    }
    public enum DivisorType
    {
        None,
        Whole,
        FirstHalf,
        SecondHalf,
    }
}
