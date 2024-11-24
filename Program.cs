using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("cs-CZ");
        Console.OutputEncoding = Encoding.UTF8;

        string data = File.ReadAllText("data.txt");

        
        Product[] products = ParseData(data);

        
        double sum = GetTotalProductsPrice(products);
        double averageWeight = GetAverageItemWeight(products);

        Console.WriteLine("Produkty:");
        foreach (var product in products)
        {
            Console.WriteLine(product.ToString());
        }

        Console.WriteLine($"\nCelková cena produktov: {sum} €");
        Console.WriteLine($"Priemerná váha položky: {averageWeight:F3} kg");
    }

    static Product[] ParseData(string data)
{
    var products = new List<Product>();
    string[] lines = data.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

    Product currentProduct = null;
    foreach (string line in lines)
    {
        if (line.StartsWith("\t - "))
        {
            string productName = line.Substring(4).TrimEnd(':');
            currentProduct = new Product { Name = productName };
            products.Add(currentProduct);
        }
        else if (line.StartsWith("\t\t - "))
        {
            if (currentProduct != null)
            {
                string[] parts = line.Substring(5).Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();
                    switch (key)
                    {
                        case "price":
                            if (double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedPrice))
                            {
                                currentProduct.Price = parsedPrice;
                            }
                            else
                            {
                                Console.WriteLine($"Warning: Could not parse price value '{value}' for product '{currentProduct?.Name}'.");
                                currentProduct.Price = 0; 
                            }


                            break;
                        case "quantity":
                            currentProduct.Quantity = int.TryParse(value, out int qty) ? qty : (int?)null;
                            break;
                        case "weight":
                            currentProduct.ProductWeight = ParseWeight(value); 
                            break;
                    }
                }
            }
        }
    }
    return products.ToArray();
}


    static Product.Weight ParseWeight(string text)
{
    string[] parts = text.Split(' ');
    if (parts.Length != 2)
    {
        throw new FormatException($"Invalid weight format: '{text}'. Expected format is '<number> <unit>'.");
    }

    if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
    {
        throw new FormatException($"Invalid weight value: '{parts[0]}' in '{text}'.");
    }

    Product.WeightUnit unit = parts[1].ToLower() switch
    {
        "g" => Product.WeightUnit.Gram,
        "dkg" => Product.WeightUnit.Decagram,
        "kg" => Product.WeightUnit.Kilogram,
        _ => throw new FormatException($"Unknown weight unit: '{parts[1]}' in '{text}'.")
    };

    return new Product.Weight { Value = value, Unit = unit };
}


    static double GetTotalProductsPrice(Product[] products)
    {
        double total = 0;
        foreach (var product in products)
        {
            if (product.Quantity.HasValue)
            {
                total += product.Price * product.Quantity.Value;
            }
        }
        return total;
    }

    static double GetAverageItemWeight(Product[] products)
{
    double totalWeight = 0;
    int itemCount = 0;

    foreach (var product in products)
    {
        if (product.ProductWeight.HasValue)
        {
            double weightInKg = product.ProductWeight.Value.GetNormalizedValue();
            int quantity = product.Quantity ?? 1;
            totalWeight += weightInKg * quantity;
            itemCount += quantity;
        }
    }

    return itemCount > 0 ? Math.Round(totalWeight / itemCount, 3) : 0;
}
}

class Product
{
    public string Name { get; set; }
    public double Price { get; set; }
    public int? Quantity { get; set; }
    public Weight? ProductWeight { get; set; }

    public enum WeightUnit
    {
        Gram,
        Decagram,
        Kilogram
    }

    public struct Weight
    {
        public double Value { get; set; }
        public WeightUnit Unit { get; set; }

        public double GetNormalizedValue()
        {
            return Unit switch
            {
                WeightUnit.Gram => Value / 1000,
                WeightUnit.Decagram => Value / 100,
                WeightUnit.Kilogram => Value
            };
        }
    }

    public override string ToString()
    {
        string quantityText = Quantity.HasValue ? $"{Quantity.Value} ks" : "neznáme množstvo";
        return $"{Name}: {quantityText}; {Price} €";
    }
}

