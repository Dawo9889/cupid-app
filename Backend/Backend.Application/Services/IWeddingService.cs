﻿using Backend.Application.DTO.WeddingDTO;
using Backend.Domain.Entities;

namespace Backend.Application.Services
{
    public interface IWeddingService
    {
        Task Create(WeddingDTO weddingDTO);
        Task<List<WeddingDTO>> GetAllWeddings();

        Task<WeddingDetailsDTO> GetWeddingDetailsById(Guid id);

        Task<bool> Delete(Guid id);

        Task<bool> Update(WeddingDTO newWeddingDTO);

    }
}