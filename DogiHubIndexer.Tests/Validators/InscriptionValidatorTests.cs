using DogiHubIndexer.Entities;
using DogiHubIndexer.Validators;
using NBitcoin;
using System.Text.Json;

namespace DogiHubIndexer.Tests.Validators
{
    public class InscriptionValidatorTests
    {
        [Fact]
        public void TryGetTokenInscriptionContent_InvalidP_ReturnsFalse()
        {
            // Arrange
            var validator = new InscriptionValidator();
            var json = "{\"p\":\"invalid\",\"op\":\"deploy\",\"tick\":\"DOGE\",\"amt\":\"100\",\"lim\":\"1\",\"max\":\"1000\"}";
            var jsonDocument = JsonDocument.Parse(json);
            var genesisTxId = new uint256();
            var inscriptionId = new InscriptionId(uint256.One, 0);

            // Act
            var result = validator.TryGetTokenInscriptionContent(DateTimeOffset.Now, jsonDocument, genesisTxId, inscriptionId, "application/json", out var inscriptionRawData);

            // Assert
            Assert.False(result);
            Assert.Null(inscriptionRawData);
        }

        [Fact]
        public void TryGetTokenInscriptionContent_InvalidOp_ReturnsFalse()
        {
            // Arrange
            var validator = new InscriptionValidator();
            var json = "{\"p\":\"drc-20\",\"op\":\"invalid_op\",\"tick\":\"DOGE\",\"amt\":\"100\",\"lim\":\"1\",\"max\":\"1000\"}";
            var jsonDocument = JsonDocument.Parse(json);
            var genesisTxId = new uint256();
            var inscriptionId = new InscriptionId(uint256.One, 0);

            // Act
            var result = validator.TryGetTokenInscriptionContent(DateTimeOffset.Now, jsonDocument, genesisTxId, inscriptionId, "application/json", out var inscriptionRawData);

            // Assert
            Assert.False(result);
            Assert.Null(inscriptionRawData);
        }

        [Theory]
        [InlineData("")] // Empty tick
        [InlineData("ABC")] // Length not equal to 4
        public void TryGetTokenInscriptionContent_InvalidTick_ReturnsFalse(string tick)
        {
            // Arrange
            var validator = new InscriptionValidator();
            var json = $"{{\"p\":\"drc-20\",\"op\":\"deploy\",\"tick\":\"{tick}\",\"amt\":\"100\",\"lim\":\"1\",\"max\":\"1000\"}}";
            var jsonDocument = JsonDocument.Parse(json);
            var genesisTxId = new uint256();
            var inscriptionId = new InscriptionId(uint256.One, 0);

            // Act
            var result = validator.TryGetTokenInscriptionContent(DateTimeOffset.Now, jsonDocument, genesisTxId, inscriptionId, "application/json", out var inscriptionRawData);

            // Assert
            Assert.False(result);
            Assert.Null(inscriptionRawData);
        }

        [Theory]
        [InlineData("deploy")]
        [InlineData("mint")]
        [InlineData("transfer")]
        public void TryGetTokenInscriptionContent_ValidOperations_ReturnsTrue(string operation)
        {
            // Arrange
            var validator = new InscriptionValidator();
            var json = $"{{\"p\":\"drc-20\",\"op\":\"{operation}\",\"tick\":\"DOGE\",\"amt\":\"100\",\"lim\":\"1\",\"max\":\"1000\"}}";
            var jsonDocument = JsonDocument.Parse(json);
            var genesisTxId = new uint256();
            var inscriptionId = new InscriptionId(uint256.One, 0);

            // Act
            var result = validator.TryGetTokenInscriptionContent(DateTimeOffset.Now, jsonDocument, genesisTxId, inscriptionId, "application/json", out var inscriptionRawData);

            // Assert
            Assert.True(result);
            Assert.NotNull(inscriptionRawData);
        }

        [Fact]
        public void TryGetTokenInscriptionContent_IncompleteData_ReturnsFalse()
        {
            // Arrange
            var validator = new InscriptionValidator();
            var json = "{\"p\":\"drc-20\"}"; // Only "p" provided, missing other required fields
            var jsonDocument = JsonDocument.Parse(json);
            var genesisTxId = new uint256();
            var inscriptionId = new InscriptionId(uint256.One, 0);

            // Act
            var result = validator.TryGetTokenInscriptionContent(DateTimeOffset.Now, jsonDocument, genesisTxId, inscriptionId, "application/json", out var inscriptionRawData);

            // Assert
            Assert.False(result);
            Assert.Null(inscriptionRawData);
        }

        [Fact]
        public void TryGetDnsInscriptionContent_InvalidP_ReturnsFalse()
        {
            var validator = new InscriptionValidator();
            var json = "{\"p\":\"invalid\",\"op\":\"reg\",\"name\":\"example.doge\"}";
            var jsonDocument = JsonDocument.Parse(json);
            var genesisTxId = new uint256();
            var inscriptionId = new InscriptionId(uint256.One, 0);

            var result = validator.TryGetDnsInscriptionContent(DateTimeOffset.Now, jsonDocument, genesisTxId, inscriptionId, "application/json", out var inscriptionRawData);

            Assert.False(result);
            Assert.Null(inscriptionRawData);
        }

        [Fact]
        public void TryGetDnsInscriptionContent_InvalidOp_ReturnsFalse()
        {
            var validator = new InscriptionValidator();
            var json = "{\"p\":\"dns\",\"op\":\"invalid\",\"name\":\"example.doge\"}";
            var jsonDocument = JsonDocument.Parse(json);
            var genesisTxId = new uint256();
            var inscriptionId = new InscriptionId(uint256.One, 0);

            var result = validator.TryGetDnsInscriptionContent(DateTimeOffset.Now, jsonDocument, genesisTxId, inscriptionId, "application/json", out var inscriptionRawData);

            Assert.False(result);
            Assert.Null(inscriptionRawData);
        }

        [Fact]
        public void TryGetDnsInscriptionContent_InvalidName_ReturnsFalse()
        {
            var validator = new InscriptionValidator();
            var json = "{\"p\":\"dns\",\"op\":\"reg\",\"name\":\"\"}";
            var jsonDocument = JsonDocument.Parse(json);
            var genesisTxId = new uint256();
            var inscriptionId = new InscriptionId(uint256.One, 0);

            var result = validator.TryGetDnsInscriptionContent(DateTimeOffset.Now, jsonDocument, genesisTxId, inscriptionId, "application/json", out var inscriptionRawData);

            Assert.False(result);
            Assert.Null(inscriptionRawData);
        }

        [Theory]
        [InlineData("example.doge")]
        [InlineData("example.hub")]
        public void TryGetDnsInscriptionContent_ValidNameAndExtension_ReturnsTrue(string name)
        {
            var validator = new InscriptionValidator();
            var json = $"{{\"p\":\"dns\",\"op\":\"reg\",\"name\":\"{name}\"}}";
            var jsonDocument = JsonDocument.Parse(json);
            var genesisTxId = new uint256();
            var inscriptionId = new InscriptionId(uint256.One, 0);

            var result = validator.TryGetDnsInscriptionContent(DateTimeOffset.Now, jsonDocument, genesisTxId, inscriptionId, "application/json", out var inscriptionRawData);

            Assert.True(result);
            Assert.NotNull(inscriptionRawData);
        }

        [Fact]
        public void TryGetDnsInscriptionContent_UnauthorizedExtension_ReturnsFalse()
        {
            var validator = new InscriptionValidator();
            var json = "{\"p\":\"dns\",\"op\":\"reg\",\"name\":\"example.unauthorized\"}";
            var jsonDocument = JsonDocument.Parse(json);
            var genesisTxId = new uint256();
            var inscriptionId = new InscriptionId(uint256.One, 0);

            var result = validator.TryGetDnsInscriptionContent(DateTimeOffset.Now, jsonDocument, genesisTxId, inscriptionId, "application/json", out var inscriptionRawData);

            Assert.False(result);
            Assert.Null(inscriptionRawData);
        }

        [Fact]
        public void TryGetNftInscriptionContent_WithData_ReturnsTrueAndCorrectData()
        {
            // Arrange
            var validator = new InscriptionValidator();
            var genesisTxId = new uint256();
            var inscriptionId = new InscriptionId(uint256.One, 0);
            var transactionIds = new List<uint256> { new uint256(1), new uint256(2) };

            // Act
            var result = validator.TryGetNftInscriptionContent(123, DateTimeOffset.Now, genesisTxId, inscriptionId, "application/json", transactionIds, true, out var inscriptionRawData);

            // Assert
            Assert.True(result);
            Assert.NotNull(inscriptionRawData);
            Assert.Equal(genesisTxId, inscriptionRawData.GenesisTxId);
            Assert.Equal(inscriptionId, inscriptionRawData.Id);
            Assert.Equal("application/json", inscriptionRawData.ContentType);
            Assert.True(inscriptionRawData.NftContent!.IsComplete);
            Assert.Equal(transactionIds, inscriptionRawData.NftContent.TxIds);
        }

        [Fact]
        public void TryGetNftInscriptionContent_EmptyTransactionIds_ReturnsTrue()
        {
            // Arrange
            var validator = new InscriptionValidator();
            var genesisTxId = new uint256();
            var inscriptionId = new InscriptionId(uint256.One, 0);
            var transactionIds = new List<uint256>();

            // Act
            var result = validator.TryGetNftInscriptionContent(123, DateTimeOffset.Now, genesisTxId, inscriptionId, "application/json", transactionIds, false, out var inscriptionRawData);

            // Assert
            Assert.True(result);
            Assert.NotNull(inscriptionRawData);
            Assert.False(inscriptionRawData.NftContent!.IsComplete);
            Assert.Empty(inscriptionRawData.NftContent.TxIds);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TryGetNftInscriptionContent_IsCompleteVariants_ReturnsTrue(bool isComplete)
        {
            // Arrange
            var validator = new InscriptionValidator();
            var genesisTxId = new uint256();
            var inscriptionId = new InscriptionId(uint256.One, 0);
            var transactionIds = new List<uint256> { new uint256(1) };

            // Act
            var result = validator.TryGetNftInscriptionContent(123, DateTimeOffset.Now, genesisTxId, inscriptionId, "application/json", transactionIds, isComplete, out var inscriptionRawData);

            // Assert
            Assert.True(result);
            Assert.NotNull(inscriptionRawData);
            Assert.Equal(isComplete, inscriptionRawData.NftContent!.IsComplete);
        }

    }
}
