using LagoVista.Core.Models;

namespace Relational.Tests.Core.Seeds
{
    public static class NonRelationalSeeds
    {
        public const string Project1Id = "PROJECT00000000000000000000000001"; 
        public const string Project2Id = "PROJECT00000000000000000000000002"; 
        public const string Project3Id = "PROJECT00000000000000000000000003"; 
        public const string Project4Id = "PROJECT00000000000000000000000004"; 
        public const string Project5Id = "PROJECT00000000000000000000000005"; 
        public const string Project6Id = "PROJECT00000000000000000000000006";
        
        public static readonly EntityHeader Project1 = EntityHeader.Create(Project1Id, "Project 1"); 
        public static readonly EntityHeader Project2 = EntityHeader.Create(Project2Id, "Project 2"); 
        public static readonly EntityHeader Project3 = EntityHeader.Create(Project3Id, "Project 3"); 
        public static readonly EntityHeader Project4 = EntityHeader.Create(Project4Id, "Project 4"); 
        public static readonly EntityHeader Project5 = EntityHeader.Create(Project5Id, "Project 5"); 
        public static readonly EntityHeader Project6 = EntityHeader.Create(Project6Id, "Project 6");

        public const string WorkTask1Id = "TASK00000000000000000000000000001"; 
        public const string WorkTask2Id = "TASK00000000000000000000000000002"; 
        public const string WorkTask3Id = "TASK00000000000000000000000000003"; 
        public const string WorkTask4Id = "TASK00000000000000000000000000004"; 
        public const string WorkTask5Id = "TASK00000000000000000000000000005"; 
        public const string WorkTask6Id = "TASK00000000000000000000000000006";
        public static readonly EntityHeader WorkTask1 = EntityHeader.Create(WorkTask1Id, "Task 1"); 
        public static readonly EntityHeader WorkTask2 = EntityHeader.Create(WorkTask2Id, "Task 2"); 
        public static readonly EntityHeader WorkTask3 = EntityHeader.Create(WorkTask3Id, "Task 3"); 
        public static readonly EntityHeader WorkTask4 = EntityHeader.Create(WorkTask4Id, "Task 4"); 
        public static readonly EntityHeader WorkTask5 = EntityHeader.Create(WorkTask5Id, "Task 5"); 
        public static readonly EntityHeader WorkTask6 = EntityHeader.Create(WorkTask6Id, "Task 6");

        public const string CreditCard1Id = "1BBDCB3A39F74D12AD84DF872250E9F9";
        public static readonly EntityHeader CreditCard1 = EntityHeader.Create(CreditCard1Id, "Credit Card 1");


        public const string CustContact1Id = "CONTACT000000000000000000000000001";
        public const string CustContact2Id = "CONTACT000000000000000000000000002";
        public const string CustContact3Id = "CONTACT000000000000000000000000003";
        public static readonly EntityHeader CustContact1 = EntityHeader.Create(CustContact1Id, "Contact 1");
        public static readonly EntityHeader CustContact2 = EntityHeader.Create(CustContact2Id, "Contact 2");
        public static readonly EntityHeader CustContact3 = EntityHeader.Create(CustContact3Id, "Contact 3");


        public const string Resource1Id = "RESOURCE0000000000000000000000001";
        public const string Resource2Id = "RESOURCE0000000000000000000000002";
        public const string Resource3Id = "RESOURCE0000000000000000000000003";
        public static readonly EntityHeader Resource1 = EntityHeader.Create(Resource1Id, "Resource 1");
        public static readonly EntityHeader Resource2 = EntityHeader.Create(Resource2Id, "Resource 2");
        public static readonly EntityHeader Resource3 = EntityHeader.Create(Resource3Id, "Resource 3");

        public const string ImageRource1Id = "IMGRESRC0000000000000000000000001";
        public const string ImageRource2Id = "IMGRESRC0000000000000000000000002";
        public static readonly EntityHeader ImageResource1 = EntityHeader.Create(ImageRource1Id, "Resource 1");
        public static readonly EntityHeader ImageResource2 = EntityHeader.Create(ImageRource2Id, "Resource 2");

        public const string ProductCatgType1Id =        "PRODCATGTYPE00000000000000000001";
        public const string ProductCatgType2Id =        "PRODCATGTYPE00000000000000000002";
        public const string ProductCatgTypeSoftwareId = "PRODCATGSFTW00000000000000000001";
        public const string ProductCatgTypeHardwareId = "PRODCATGHDWR00000000000000000001";
        public const string ProductCatgAiID =           "PRODCATGAIAI00000000000000000001";

        public static readonly EntityHeader ProductCatgType1 = EntityHeader.Create(ProductCatgType1Id, "Product Category Type 1");
        public static readonly EntityHeader ProductCatgType2 = EntityHeader.Create(ProductCatgType2Id, "Product Category Type 2");
        public static readonly EntityHeader ProductCatgTypeSoftware = EntityHeader.Create(ProductCatgType2Id, "Software");
        public static readonly EntityHeader ProductCatgTypeHardware = EntityHeader.Create(ProductCatgTypeHardwareId, "Hardware");
        public static readonly EntityHeader ProductCatgTypeAI = EntityHeader.Create(ProductCatgAiID, "Ai");

    }
}
