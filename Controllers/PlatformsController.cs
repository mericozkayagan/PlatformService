using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using PlatformService.Data;
using System.Collections.Generic;
using PlatformService.Dtos;
using System;
using PlatformService.Models;
using PlatformService.SyncDataServices.Http;
using System.Threading.Tasks;
using PlatformService.AsyncDataServices;

namespace PlatformService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlatformsController : ControllerBase
    {
        private readonly IPlatformRepo _repository;
        private readonly IMapper _mapper;
        private readonly ICommandDataClient _commandDataClient;
        private readonly IMessageBusClient _messageBusClient;

        public PlatformsController(IPlatformRepo repository, 
        IMapper mapper, 
        ICommandDataClient commandDataClient,
        IMessageBusClient messageBusClient)
        {
            _repository = repository;
            _mapper = mapper;
            _commandDataClient = commandDataClient;
            _messageBusClient = messageBusClient;
        }

        [HttpGet]
        public ActionResult<IEnumerable<PlatformReadDto>> GetPlatforms()
        {
            Console.WriteLine("--> getting platforms");
            var platformItem = _repository.GetAllPlatforms();

            return Ok(_mapper.Map<IEnumerable<PlatformReadDto>>(platformItem));
        }

        [HttpGet("{id}", Name = "GetPlatformById")]
        public ActionResult<PlatformReadDto> GetPlatformById(int id)
        {
            Console.WriteLine("--> getting platforms");
            var platformItem = _repository.GetPlatformById(id);
            if (platformItem is null)
                return NotFound();

            return Ok(_mapper.Map<PlatformReadDto>(platformItem));
        }

        [HttpPost]
        public async Task<ActionResult<PlatformReadDto>> CreatePlatform(PlatformCreateDto p)
        {
            var platform = _mapper.Map<Platform>(p);
            _repository.CreatePlatform(platform);
            _repository.SaveChanges();

            var readDto = _mapper.Map<PlatformReadDto>(platform);

            try
            {
                await _commandDataClient.SendPlatformToCommand(readDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not send synchronusly: {ex.Message}");
            }

            try
            {
                var platformPublishedDto = _mapper.Map<PlatformPublishedDto>(readDto);
                platformPublishedDto.Event = "Platform published";
                _messageBusClient.PublishNewPlatform(platformPublishedDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not send asynchronusly: {ex.Message}");
            }

            return CreatedAtRoute(nameof(GetPlatformById), new { Id = readDto.Id}, readDto);
        }
    }
}