using Castle.Core.Resource;
using LagoVista;
using LagoVista.CloudStorage.Utils;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.IoT.Logging;
using LagoVista.Relational.DataContexts;
using LagoVista.Security;
using LagoVista.UserAdmin.Interfaces.Repos.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static MarchDataMigration.Decryption.EfDatabase;

namespace MarchDataMigration.Decryption
{
    public class EfDatabase
    {
        [SetUp]
        public async Task Init()
        {
            var uid = Environment.GetEnvironmentVariable("SQL_READER_USER");
            var pwd = Environment.GetEnvironmentVariable("SQL_READER_PASSWORD");
            var catalog = "nuviot-dev-2026-03-22";
            var connectionString = Utils.ConnectionStrings.Build(TestConnections.DevSQLServer, userName: null, password: null, initialCatalog: catalog);

            var builder = new DbContextOptionsBuilder<BillingDataContext>();
            builder.UseSqlServer(connectionString);
            Ctx = new BillingDataContext(builder.Options);
        }

        protected BillingDataContext Ctx { get; private set; }

        protected string DecryptString(string id, string rate, string key)
        {
            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(rate);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))
                        {
                            var plainText = streamReader.ReadToEnd();
                            return plainText.Replace(id, String.Empty);
                        }
                    }
                }
            }
        }

        [Test]
        public async Task GetInvoices()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var settings = new EncryptionSettings();
            var aesProvider = new AESEncryptionProvider(settings, new ConsoleLogger());

            var secureStorage = new SecureStorage(new EncryptionSettings(), new ConsoleLogger(), Mock.Of<IAccessLogRepo>(), aesProvider, new ConfigKeyRing(settings), new AesGcmSecretEnvelopeCrypto(), cache);


            var invoices = await Ctx.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Organization)
                .Include(i => i.Agreement).ThenInclude(a => a.CreatedByUser)
                .OrderBy(i => i.InvoiceDate)
                .ToListAsync();
            foreach (var invoice in invoices)
            {
                var org = EntityHeader.Create(invoice.OrganizationId, "dontcare");
                var user = EntityHeader.Create("E1D8DFF5959A420D93289130CCAB1675", "dontcare");
                var key = $"Agreement-{invoice.CustomerId}";
                var secretResult = await secureStorage.GetSecretAsync(invoice.Organization.ToEntityHeader(), key, invoice.Agreement.CreatedByUser.ToEntityHeader());

                try
                {
                    var actualAmount = DecryptString(invoice.Id.ToString(), invoice.EncryptedTotal, secretResult.Result);
                    Console.WriteLine($"Invoice {invoice.InvoiceDate}  for {invoice.CustomerName} amount {actualAmount}");
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Invoice {invoice.InvoiceDate} for {invoice.CustomerName} failed to decrypt: {ex.Message} (Encryption Text: {invoice.EncryptedTotal})");
                }
            }
        }

        protected string GetExpensessSecretId(string orgId, string id)
        {
            if (String.IsNullOrEmpty(orgId))
                throw new ArgumentNullException("Must provide an id to get expense secret id.");

            if (String.IsNullOrEmpty(id))
                throw new ArgumentNullException("Must provide an id to get expense secret id.");

            return $"ExpenseRecordKey-{orgId}-{id}";
        }

        [Test]
        public async Task GetExpenses()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var settings = new EncryptionSettings();
            var aesProvider = new AESEncryptionProvider(settings, new ConsoleLogger());

            var secureStorage = new SecureStorage(new EncryptionSettings(), new ConsoleLogger(), Mock.Of<IAccessLogRepo>(), aesProvider, new ConfigKeyRing(settings), new AesGcmSecretEnvelopeCrypto(), cache);




            var expenses = await Ctx.Expenses
                .Include(i => i.User)
                .Include(i => i.Organization)
                .OrderBy(i => i.ExpenseDate)
                .ToListAsync();
            foreach (var exp in expenses)
            {
                var key = GetExpensessSecretId(exp.OrganizationId, exp.UserId);
                var secretResult = await secureStorage.GetSecretAsync(exp.Organization.ToEntityHeader(), key, exp.User.ToEntityHeader());

                try
                {
                    var actualAmount = DecryptString(exp.Id.ToString(), exp.EncryptedAmount, secretResult.Result);
                    Console.WriteLine($"Expense {exp.ExpenseDate}/{exp.User.FullName} for {exp.Description} amount {actualAmount}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Invoice  {exp.ExpenseDate}/{exp.User.FullName} failed to decrypt: {ex.Message} (Encryption Text: {exp.EncryptedAmount})");
                }
            }
        }

        [TearDown]
        public void Cleanup()
        {
            Ctx.Dispose();
        }

        public class EncryptionSettings : ISecureStorageSettings
        {
            public const string KEY_ID = "kek-2026-03-22";

            public IConnectionSettings ConnectionSettings => LagoVista.CloudStorage.Utils.TestConnections.ProductionTableStorageDB;

            public IConnectionSettings LegacyConnectionSettings => LagoVista.CloudStorage.Utils.TestConnections.ProductionTableStorageDB;

            public string EncryptionKey => System.Environment.GetEnvironmentVariable("PROD_TEST_ID", EnvironmentVariableTarget.User);

            public string LegacySecret => "dontcare";

            public string ActiveKeyId => KEY_ID;

            private static string RandomKey()
            {
                return Environment.GetEnvironmentVariable("TEST_PROD_ID_3");
            }

            private List<SecureStorageKeySettings> _keys = new List<SecureStorageKeySettings>
        {
            new SecureStorageKeySettings
            {
                KeyId = KEY_ID,
                KeyMaterial = RandomKey(),
                IsActive = true,
                CanDecrypt = true
            }
        };

            public IList<SecureStorageKeySettings> Keys => _keys;
        }
    }
}
