using System;
using System.Net;
using System.Text.Json;
using RestSharp;
using RestSharp.Authenticators;
using MovieCatalog.Models;



namespace MovieCatalog

{
    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string lastCreateadMovieId;

        private const string BaseUrl = "http://144.91.123.158:5000";
        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiIyYWY1YWEzMy0wNmNiLTQ0M2MtOWE3NS1lODgwNTg1ZWY0NTAiLCJpYXQiOiIwNC8xOC8yMDI2IDA2OjMwOjA5IiwiVXNlcklkIjoiZjQwYjlkN2MtNDNkYS00NGUzLTYxOTctMDhkZTc2OTcxYWI5IiwiRW1haWwiOiJyYWxseUBleGFtcGxlLmNvbSIsIlVzZXJOYW1lIjoiUmFsbHkiLCJleHAiOjE3NzY1MTU0MDksImlzcyI6Ik1vdmllQ2F0YWxvZ19BcHBfU29mdFVuaSIsImF1ZCI6Ik1vdmllQ2F0YWxvZ19XZWJBUElfU29mdFVuaSJ9.BOhZymtrNsAMl6O1WOXCI088BBImHiT4hrFu7uZWFew";

        private const string LoginEmail = "rally@example.com";
        private const string LoginPassword = "123456";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("token").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found in the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateNewMovie_WithRequiredFields_ShouldReturnSuccess()
        {
            var movieData = new MovieDTO
            {
                Title = "Test Movie",
                Description = "This is a test movie description."
            };

            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movieData);

            var response = this.client.Execute(request);

            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(
                response.Content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(createResponse.Msg, Is.EqualTo("Movie created successfully!"));
            Assert.That(createResponse.Movie, Is.Not.Null);
            Assert.That(createResponse.Movie.Id, Is.Not.Null.And.Not.Empty);

            lastCreateadMovieId = createResponse.Movie.Id;
        }

        [Order(2)]
        [Test]
        public void EditExistingMovie_ShouldReturnSuccess()
        {
            var editData = new MovieDTO
            {
                Title = "Edited Movie",
                Description = "This is an edited movie description."
            };

            var request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", lastCreateadMovieId);
            request.AddJsonBody(editData);

            var response = this.client.Execute(request);

            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(
                response.Content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponse.Msg, Is.EqualTo("Movie edited successfully!"));
        }

        [Order(3)]
        [Test]
        public void GetAllMovies_ShouldReturnNonEmptyArray()
        {
            var request = new RestRequest("/api/Catalog/All", Method.Get);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            Assert.That(response.Content, Is.Not.Null.And.Not.Empty,
                "API returned empty response (not valid JSON array)");

            var movies = JsonSerializer.Deserialize<List<object>>(response.Content);

            Assert.That(movies, Is.Not.Null);
            Assert.That(movies, Is.Not.Empty);
        }

        [Test]
        [Order(4)]
        public void DeleteCreatedMovie_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", lastCreateadMovieId);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var result = JsonSerializer.Deserialize<ApiResponseDTO>(
                response.Content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(result.Msg, Is.EqualTo("Movie deleted successfully!"));
        }

        [Test]
        [Order(5)]
        public void CreateMovie_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var movie = new MovieDTO
            {
                Title = "",
                Description = ""
            };

            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movie);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        [Order(6)]
        public void EditNonExistingMovie_ShouldReturnBadRequest()
        {
            var editData = new MovieDTO
            {
                Title = "Non Existing Movie",
                Description = "This movie does not exist"
            };

            var request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", "invalid-id-12345");
            request.AddJsonBody(editData);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            var result = JsonSerializer.Deserialize<ApiResponseDTO>(
                response.Content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            Assert.That(result.Msg, Is.EqualTo("Unable to edit the movie! Check the movieId parameter or user verification!"));
        }

        [Test]
        [Order(7)]
        public void DeleteNonExistingMovie_ShouldReturnBadRequest()
        {
            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", "invalid-id-12345");

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            var result = JsonSerializer.Deserialize<ApiResponseDTO>(
                response.Content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            Assert.That(result.Msg, Is.EqualTo("Unable to delete the movie! Check the movieId parameter or user verification!"));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}