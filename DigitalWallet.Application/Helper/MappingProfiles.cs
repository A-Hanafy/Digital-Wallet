using AutoMapper;
using DigitalWallet.Application.DTOs;
using DigitalWallet.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWallet.Application.Helper
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
            CreateMap<Wallet, WalletDto>().ReverseMap();
            CreateMap<Transaction, TransactionDto>().ReverseMap();
        }
    }
}
