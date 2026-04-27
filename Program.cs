using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Data.Sqlite;

namespace IDZ_2
{
    // === Часть 2. Классы-модели  ===
    public class Brand
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Brand(int id, string name) { Id = id; Name = name; }
        public Brand() : this(0, "") { }
        public override string ToString() => $"[{Id}] {Name}";
    }

    public class Car
    {
        public int Id { get; set; }
        public int BrandId { get; set; }
        public string ModelName { get; set; }
        private int _horsepower;
        public int Horsepower
        {
            get => _horsepower;
            set
            {
                if (value < 0) throw new ArgumentException("Мощность не может быть отрицательной [cite: 275]");
                _horsepower = value;
            }
        }
        public Car(int id, int brandId, string modelName, int horsepower)
        {
            Id = id; BrandId = brandId; ModelName = modelName; Horsepower = horsepower;
        }
        public Car() : this(0, 0, "", 0) { }
        public override string ToString() => $"[{Id}] {ModelName}, Марка #{BrandId}, Мощность: {Horsepower} л.с.";
    }

    // === Часть 3. Класс DatabaseManager ===
    public class DatabaseManager
    {
        private string _connectionString;
        public DatabaseManager(string dbPath) => _connectionString = $"Data Source={dbPath}";

        public void InitializeDatabase(string brandCsv, string carCsv)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS brands (brand_id INTEGER PRIMARY KEY, brand_name TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS cars (car_id INTEGER PRIMARY KEY AUTOINCREMENT, brand_id INTEGER NOT NULL, 
                model_name TEXT NOT NULL, horsepower INTEGER NOT NULL, FOREIGN KEY (brand_id) REFERENCES brands(brand_id));";
            cmd.ExecuteNonQuery();

            // Автоматический импорт из CSV
            if (GetAllBrands().Count == 0 && File.Exists(brandCsv)) ImportBrands(brandCsv);
            if (GetAllCars().Count == 0 && File.Exists(carCsv)) ImportCars(carCsv);
        }

        private void ImportBrands(string path)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            foreach (var line in File.ReadAllLines(path).Skip(1))
            {
                var p = line.Split(';');
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO brands (brand_id, brand_name) VALUES (@id, @n)";
                cmd.Parameters.AddWithValue("@id", int.Parse(p[0]));
                cmd.Parameters.AddWithValue("@n", p[1]);
                cmd.ExecuteNonQuery();
            }
        }

        private void ImportCars(string path)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            foreach (var line in File.ReadAllLines(path).Skip(1))
            {
                var p = line.Split(';');
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO cars (car_id, brand_id, model_name, horsepower) VALUES (@id, @bid, @n, @hp)";
                cmd.Parameters.AddWithValue("@id", int.Parse(p[0]));
                cmd.Parameters.AddWithValue("@bid", int.Parse(p[1]));
                cmd.Parameters.AddWithValue("@n", p[2]);
                cmd.Parameters.AddWithValue("@hp", int.Parse(p[3]));
                cmd.ExecuteNonQuery();
            }
        }

        public List<Brand> GetAllBrands()
        {
            var list = new List<Brand>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM brands";
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new Brand(r.GetInt32(0), r.GetString(1)));
            return list;
        }

        public List<Car> GetAllCars()
        {
            var list = new List<Car>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM cars";
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new Car(r.GetInt32(0), r.GetInt32(1), r.GetString(2), r.GetInt32(3)));
            return list;
        }

        public void AddCar(Car c)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO cars (brand_id, model_name, horsepower) VALUES (@bid, @n, @hp)";
            cmd.Parameters.AddWithValue("@bid", c.BrandId);
            cmd.Parameters.AddWithValue("@n", c.ModelName);
            cmd.Parameters.AddWithValue("@hp", c.Horsepower);
            cmd.ExecuteNonQuery();
        }
    }

    // === Основная программа и Меню ===
    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            string dbPath = "cars.db";
            string brandCsv = Path.Combine(AppContext.BaseDirectory, "brands.csv");
            string carCsv = Path.Combine(AppContext.BaseDirectory, "cars.csv");

            var db = new DatabaseManager(dbPath);
            db.InitializeDatabase(brandCsv, carCsv);

            string choice;
            do
            {
                Console.WriteLine("\n=== УПРАВЛЕНИЕ АВТОПАРКОМ ===\n1. Показать марки\n2. Показать автомобили\n3. Добавить авто\n0. Выход");
                Console.Write("Ваш выбор: ");
                choice = Console.ReadLine();

                switch (choice)
                {
                    case "1": ShowBrands(db); break;
                    case "2": ShowCars(db); break;
                    case "3": AddCarMenu(db); break;
                }
            } while (choice != "0");
        }

        static void ShowBrands(DatabaseManager db) => db.GetAllBrands().ForEach(b => Console.WriteLine(b));
        static void ShowCars(DatabaseManager db) => db.GetAllCars().ForEach(c => Console.WriteLine(c));

        static void AddCarMenu(DatabaseManager db)
        {
            try
            {
                Console.WriteLine("Доступные марки:");
                db.GetAllBrands().ForEach(b => Console.WriteLine(b));
                Console.Write("ID марки: "); int bId = int.Parse(Console.ReadLine());
                Console.Write("Модель: "); string model = Console.ReadLine();
                Console.Write("Мощность: "); int hp = int.Parse(Console.ReadLine());
                db.AddCar(new Car(0, bId, model, hp));
                Console.WriteLine("Автомобиль добавлен!");
            }
            catch (Exception ex) { Console.WriteLine($"Ошибка: {ex.Message}"); }
        }
    }
}