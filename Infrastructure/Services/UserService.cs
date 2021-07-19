﻿using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ApplicationCore.Entities;
using ApplicationCore.Exceptions;
using ApplicationCore.Models;
using ApplicationCore.RepositoryInterface;
using ApplicationCore.ServiceInterface;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Infrastructure.Services
{
    public class UserService:IUserService
    {
        private IUserRepository _userRepository;
        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserLoginResponseModel> Login(string email, string password)
        {
            var dbUser = await _userRepository.GetUserByEmail(email);
            if (dbUser==null)
            {
                throw new NotFoundException("Email does not exists, Please register first");
            }
            var hashedPassword = HashPassword(password,dbUser.Salt);
            if (hashedPassword == dbUser.HashedPassword)
            {
                //correct password
                var userLoginResponse = new UserLoginResponseModel {
                    Id=dbUser.Id,
                    Email=dbUser.Email,
                    FirstName=dbUser.FirstName,
                    DateOfBirth=dbUser.DateOfBirth,
                    LastName=dbUser.LastName
                };
            }
            return null;
        }

        public async Task<UserRegisterResponseModel> RegisterUser(UserRegisterRequestModel requestModel)
        {
            //make sure email dose not exist in database(User table)
            var dbUser = await _userRepository.GetUserByEmail(requestModel.Email);
            if (dbUser!=null)
            {
                //we already have user with same email
                throw new ConflictException("Email Already Exists");
            }
            //create a unique salt, using Microsoft.AspNetCore.Cryptography.KeyDerivation;
            var salt = CreateSalt();
            var hashedPassword = HashPassword(requestModel.Password,salt);
            //save to db
            var user = new User
            {
                Email = requestModel.Email,
                Salt=salt,
                FirstName=requestModel.FirstName,
                LastName=requestModel.LastName,
                HashedPassword=hashedPassword
            };
            //save to db by calling UserRepository add method
            var createUser = await _userRepository.AddAsync(user);
            var userResponse = new UserRegisterResponseModel {
                Id=createUser.Id,
                Email=createUser.Email,
                FirstName=createUser.FirstName,
                LastName=createUser.LastName
            };
            return userResponse;
        }
        //never write security by you own
        private string CreateSalt()
        {
            byte[] randomBytes = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            return Convert.ToBase64String(randomBytes);
        }
        private string HashPassword(string password,string salt)
        {
            //Aarogon
            //Pbkdf2
            //BCrypt
            var hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                                                                    password: password,
                                                                    salt: Convert.FromBase64String(salt),
                                                                    prf: KeyDerivationPrf.HMACSHA512,
                                                                    iterationCount: 10000,
                                                                    numBytesRequested: 256 / 8));
            return hashed;
        }
    }
}