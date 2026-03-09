using Cysharp.Threading.Tasks;

namespace _Project.Scripts.Architecture.Services
{
    public interface IService
    {
        abstract UniTask Initialize();
        virtual void PostInitialize(){}
        virtual void Dispose(){}
    }
}