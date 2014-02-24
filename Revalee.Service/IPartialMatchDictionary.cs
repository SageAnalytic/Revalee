namespace Revalee.Service
{
	internal interface IPartialMatchDictionary<KeyType, ValueType>
	{
		ValueType Match(KeyType key);
	}
}