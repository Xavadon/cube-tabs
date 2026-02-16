using Cysharp.Threading.Tasks;

namespace _Project.Scripts.Architecture.Services
{
    public interface IService
    {
        public abstract UniTask Initialize();
        public virtual void PostInitialize(){}
        public virtual void Dispose(){}
    }
}