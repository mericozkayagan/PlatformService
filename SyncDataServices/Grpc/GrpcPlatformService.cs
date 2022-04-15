using AutoMapper;
using PlatformService.Data;
using Grpc.Core;
using System.Threading.Tasks;

namespace PlatformService.SyncDataServices.Grpc
{
    public class GrpcPlatformService : GrpcPlatformService.GrpcPlatformBase
    {
        public GrpcPlatformService(IPlatformRepo repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public override Task<PlatformResponse> GetAllPlatforms(GetAllRequest request, ServerCallContext context)
        {
            var response = new PlatformResponse();
            var platforms = _repository.GetAllPlatforms();

            foreach (var plat in platforms)
            {
                response.Platform.Add(_mapper.Map<GrpcPlatformModel>(plat));
            }

            return Task.FromResult(response);
        }
    }
}