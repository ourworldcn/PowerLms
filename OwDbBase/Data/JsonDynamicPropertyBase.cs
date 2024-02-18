using Microsoft.EntityFrameworkCore;
using OW.Data;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OW.Data
{

    /// <summary>
    /// 使用Json字符串存储动态对象的接口。
    /// </summary>
    public interface IJsonDynamicProperty
    {
        /// <summary>
        /// Json字符串。
        /// </summary>
        string JsonObjectString { get; set; }

        /// <summary>
        /// 存储最后一次获取对象的类型。
        /// </summary>
        [NotMapped]
        Type JsonObjectType { get; }

        /// <summary>
        /// Json字符串代表的对象。请调用<see cref="GetJsonObject{T}"/>生成该属性值。
        /// </summary>
        [NotMapped]
        abstract object JsonObject { get; set; }

        /// <summary>
        /// 将<see cref="JsonObjectString"/>解释为指定类型的对象。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        abstract T GetJsonObject<T>() where T : new();
    }

    /// <summary>
    /// 使用Json字符串存储一些动态属性的数据库类。
    /// </summary>
    public class JsonDynamicPropertyBase : GuidKeyObjectBase, IDisposable, IBeforeSave, IJsonDynamicProperty
    {
        #region 静态成员
        static JsonDynamicPropertyBase()
        {
            _SerializerOptions.Converters.Add(new OwGuidJsonConverter());
        }

        public static readonly JsonSerializerOptions _SerializerOptions = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        };

        #endregion 静态成员

        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        public JsonDynamicPropertyBase()
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id"><inheritdoc/></param>
        public JsonDynamicPropertyBase(Guid id) : base(id)
        {
        }

        #endregion 构造函数

        #region 数据库属性

        private string _JsonObjectString;
        /// <summary>
        /// 属性字符串。格式数Json字符串。
        /// </summary>
        [Column(Order = 10)]
        public string JsonObjectString
        {
            get => _JsonObjectString;
            set
            {
                if (!ReferenceEquals(_JsonObjectString, value))
                {
                    _JsonObjectString = value;
                    JsonObject = null;
                    _JsonObjectType = null;
                }
            }
        }

        #endregion 数据库属性

        #region JsonObject相关

        /// <summary>
        /// 获取或初始化<see cref="JsonObject"/>属性并返回。
        /// </summary>
        /// <typeparam name="T">若支持<see cref="INotifyPropertyChanged"/>接口则可以获得优化。</typeparam>
        /// <returns>返回的对象，不会返回null，可能返回默认的新对象。</returns>
        public T GetJsonObject<T>() where T : new()
        {
            var result = GetJsonObject(typeof(T));
            return (T)result;
        }

        /// <summary>
        /// 获取或初始化<see cref="JsonObject"/>属性并返回。
        /// </summary>
        /// <param name="type">若支持<see cref="INotifyPropertyChanged"/>接口则可以获得优化。</param>
        /// <returns>返回的对象，不会返回null，可能返回默认的新对象。</returns>
        public virtual object GetJsonObject(Type type)
        {
            //Lazy Initializer.EnsureInitialized(ref _JsonObject,);
            if (JsonObject is null || !type.IsAssignableFrom(JsonObjectType))  //若需要初始化
            {
                if (string.IsNullOrWhiteSpace(JsonObjectString))    //若Json字符串是无效的
                {
                    JsonObject = TypeDescriptor.CreateInstance(null, type, null, null);
                }
                else
                {
                    JsonObject = JsonSerializer.Deserialize(JsonObjectString, type, _SerializerOptions);
                    _Seq = _WritedSeq = 0;
                }
                _JsonObjectType = type;
            }
            return JsonObject;
        }

        volatile int _Seq;

        /// <summary>
        /// 递增属性版本号。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private void Changed_PropertyChanged(object sender, PropertyChangedEventArgs e) => Interlocked.Increment(ref _Seq);

        private object _JsonObject;
        /// <summary>
        /// 用<see cref="GetJsonObject{T}"/>获取。
        /// 甚至该属性将自动处理事件挂钩和版本号。
        /// 但是不会联动<see cref="JsonObjectType"/>属性。
        /// </summary>
        [JsonIgnore, NotMapped]
        public object JsonObject
        {
            get => _JsonObject;
            set
            {
                if (ReferenceEquals(_JsonObject, value))    //若无需设置
                    return;
                if (_JsonObject is INotifyPropertyChanged changedEvent) //若需要去除事件处理委托
                    changedEvent.PropertyChanged -= Changed_PropertyChanged;
                _JsonObject = value;
                _Seq = 1;
                _WritedSeq = 0;
                if (_JsonObject is INotifyPropertyChanged changed)  //若需要挂接事件处理委托
                    changed.PropertyChanged += Changed_PropertyChanged;
            }
        }

        private Type _JsonObjectType;
        [JsonIgnore, NotMapped]
        public Type JsonObjectType { get => _JsonObjectType; }

        #endregion JsonObject相关

        #region IDisposable接口及相关

        private volatile bool _IsDisposed;

        /// <summary>
        /// 对象是否已经被处置。
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public bool IsDisposed
        {
            get => _IsDisposed;
            protected set => _IsDisposed = value;
        }

        /// <summary>
        /// 实际处置当前对象的方法。
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_IsDisposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                    if (_JsonObject is INotifyPropertyChanged changedEvent)    //若需要去除事件处理委托
                        changedEvent.PropertyChanged -= Changed_PropertyChanged;
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _JsonObjectType = null;
                _JsonObject = null;
                _JsonObjectString = null;
                _IsDisposed = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~SimpleDynamicPropertyBase()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        /// <summary>
        /// 处置对象。
        /// </summary>
        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable接口及相关

        #region IBeforeSave接口及相关

        volatile int _WritedSeq;

        public virtual void PrepareSaving(DbContext db)
        {
            //if (_JsonObject is null)    //若是空对象 则说明未初始化，故无须序列化
            //    _JsonObjectString = null;
            //else //若对象非空
            //if (/*_JsonObject is not INotifyPropertyChanged ||*/ _Seq != _WritedSeq)
            if (_JsonObject is not null)
            {
                _JsonObjectString = JsonSerializer.Serialize(_JsonObject, JsonObjectType ?? JsonObject.GetType(), _SerializerOptions);
                if (_JsonObject is INotifyPropertyChanged)
                    _WritedSeq = _Seq;
            }
        }

        #endregion IBeforeSave接口及相关
    }
}