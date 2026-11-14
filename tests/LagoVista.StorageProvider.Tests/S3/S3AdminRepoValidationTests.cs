using LagoVista.CloudStorage.Storage.StorageProviders.File;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LagoVista.StorageProvider.Tests.S3
{
    [TestClass]
    public sealed class S3AdminRepoValidationTests
    {
        [TestMethod]
        public void FolderPrefixIsCanonical()
        {
            Assert.AreEqual("", S3AdminRepo.NormalizePrefix(null));
            Assert.AreEqual("media/", S3AdminRepo.NormalizePrefix("media"));
            Assert.AreEqual("media/images/", S3AdminRepo.NormalizePrefix("/media/images/"));
        }

        [TestMethod]
        public void ObjectKeysMustBePresent()
        {
            Assert.ThrowsException<ArgumentException>(() => S3AdminRepo.ValidateKey(""));
            Assert.ThrowsException<ArgumentException>(() => S3AdminRepo.ValidateKey("/absolute"));
            S3AdminRepo.ValidateKey("nested/file.jpg");
        }

        [TestMethod]
        public void BucketIsRequired()
        {
            Assert.ThrowsException<ArgumentException>(() => S3AdminRepo.ValidateBucket(""));
            S3AdminRepo.ValidateBucket("media");
        }
    }
}
