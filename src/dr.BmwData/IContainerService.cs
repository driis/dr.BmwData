using dr.BmwData.Models;

namespace dr.BmwData;

public interface IContainerService
{
    Task<ContainerResponse> CreateContainerAsync(string[] technicalDescriptors);
    Task<ContainerListResponse> ListContainersAsync();
    Task<ContainerResponse> GetContainerAsync(string containerId);
    Task DeleteContainerAsync(string containerId);
}
