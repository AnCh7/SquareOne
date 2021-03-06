using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;
using Sq1.Core.Broker;
using Sq1.Core.DataTypes;
using Sq1.Core.Repositories;
using Sq1.Core.Static;
using Sq1.Core.Streaming;
using Sq1.Core.Support;

namespace Sq1.Core.DataFeed {
	public class DataSource : NamedObjectJsonSerializable {
		// MOVED_TO_PARENT_NamedObjectJsonSerializable [DataMember] public new string Name;
		public string SymbolSelected;
		public List<string> Symbols;
		[JsonIgnore]
		public string SymbolsCSV {
			get {
				StringBuilder stringBuilder = new StringBuilder();
				foreach (string current in Symbols) {
					if (stringBuilder.Length > 0) stringBuilder.Append(",");
					stringBuilder.Append(current);
				}
				return stringBuilder.ToString();
			}
		}
		public BarScaleInterval ScaleInterval;
		public StaticProvider StaticProvider;
		public StreamingProvider StreamingProvider;
		public BrokerProvider BrokerProvider;
		public string MarketName;
		[JsonIgnore]
		public MarketInfo marketInfo;
		[JsonIgnore]
		public MarketInfo MarketInfo {
			get { return this.marketInfo; }
			set {
				this.marketInfo = value;
				MarketName = value.Name;
			}
		}
		public string StaticProviderName {
			get {
				if (StaticProvider == null) return "STATIC_PROVIDER_NOT_INITIALIZED";
				//return staticProvider.GetType().Name;
				return StaticProvider.Name;
			}
		}
		public string StreamingProviderName {
			get {
				if (StreamingProvider == null) return "STREAMING_PROVIDER_NOT_INITIALIZED";
				//return staticProvider.GetType().Name;
				return StreamingProvider.Name;
			}
		}
		public string BrokerProviderName {
			get {
				if (BrokerProvider == null) return "BROKER_PROVIDER_NOT_INITIALIZED";
				//return staticProvider.GetType().Name;
				return BrokerProvider.Name;
			}
		}
		[JsonIgnore]
		public bool IsIntraday { get { return ScaleInterval.IsIntraday; } }

		[JsonIgnore]
		public RepositoryBarsSameScaleInterval BarsRepository { get; protected set; }
		//public BarsFolder BarsFolderPerst { get; protected set; }

		public string DataSourceAbspath { get; protected set; }
		
		[JsonIgnore]
		public string DataSourcesAbspath;

		// used only by JsonDeserialize()
		public DataSource() {
			Name = "";
			MarketName = "";
			Symbols = new List<string>();
			SymbolSelected = "";
			DataSourceAbspath = "DATASOURCE_INITIALIZE_NOT_INVOKED_YET";
		}
		
		// should be used by a programmer
		public DataSource(string name, BarScaleInterval scaleInterval = null, MarketInfo marketInfo = null) : this() {
			this.Name = name;
			if (scaleInterval == null) {
				scaleInterval = new BarScaleInterval(BarScale.Minute, 5);
			}
			this.ScaleInterval = scaleInterval; 
			if (marketInfo == null) {
				marketInfo = Assembler.InstanceInitialized.MarketInfoRepository.FindMarketInfoOrNew("MOCK"); 
			}
			this.MarketInfo = marketInfo; 
		}
		public void Initialize(string dataSourcesAbspath, OrderProcessor orderProcessor, IStatusReporter statusReporter) {
			//if (this.HasBarDataStore) {
			//    this.BarsFile = new BarsFile(FolderForBarDataStore);
			//}
			this.DataSourcesAbspath = dataSourcesAbspath;
			this.DataSourceAbspath = Path.Combine(this.DataSourcesAbspath, this.Name);
			this.BarsRepository = new RepositoryBarsSameScaleInterval(this.DataSourceAbspath, this.ScaleInterval, true);
			
			//this.BarsFolderPerst = new BarsFolder(this.FolderForBarDataStore, this.ScaleInterval, true, "dts");

			// works only for deserialized providers; for a newDataSource they are NULLs to be assigned in DataSourceEditor 
			if (this.StaticProvider != null) {
				this.StaticProvider.Initialize(this, dataSourcesAbspath);
			}
			if (this.StreamingProvider != null) {
				this.StreamingProvider.Initialize(this, statusReporter);
				if (this.BrokerProvider != null) {
					this.BrokerProvider.Initialize(this, this.StreamingProvider, orderProcessor, statusReporter);
				}
			}
		}
		public override string ToString() {
			return Name + "(" + this.ScaleInterval.ToString() + ")" + SymbolsCSV
				+ " {" + StaticProviderName + ":" + StreamingProviderName + ":" + BrokerProviderName + "}";
		}

		// internal => use only RepositoryJsonDataSource.SymbolAdd() which will notify subscribers about add operation
		internal void SymbolAdd(string symbolToAdd) {
			if (this.Symbols.Contains(symbolToAdd)) {
				throw new Exception("ALREADY_EXISTS[" + symbolToAdd + "]");
			}
			this.BarsRepository.SymbolDataFileAdd(symbolToAdd);
			this.Symbols.Add(symbolToAdd);
		}
		// internal => use only RepositoryJsonDataSource.SymbolRename() which will notify subscribers about rename operation
		internal void SymbolRename(string oldSymbolName, string newSymbolName) {
			// nope StaticProvider can subscribe to dataSourceRepository_OnSymbolRenamed() as well and do 
			//if (this.StaticProvider != null) {
			//    this.StaticProvider.SymbolRename(oldSymbolName, newSymbolName);
			//    this.Symbols = this.StaticProvider.SymbolsStored;
			//    return;
			//}

			if (this.Symbols.Contains(oldSymbolName) == false) {
				throw new Exception("OLD_SYMBOL_DOESNT_EXIST[" + oldSymbolName + "] in [" + this.Name + "]");
			}
			if (this.Symbols.Contains(newSymbolName)) {
				throw new Exception("NEW_SYMBOL_ALREADY_EXISTS[" + newSymbolName + "] in [" + this.Name + "]");
			}

			var replacement = new List<string>();
			foreach (var symbol in this.Symbols) {
				var symbolCopy = symbol;
				if (symbolCopy == oldSymbolName) {
					symbolCopy = newSymbolName;
					this.BarsRepository.SymbolDataFileRename(oldSymbolName, newSymbolName);
				}
				replacement.Add(symbolCopy);
			}
			this.Symbols = replacement;
		}
		// internal => use only RepositoryJsonDataSource.SymbolRemove() which will notify subscribers about remove operation
		internal void SymbolRemove(string symbolToDelete) {
			if (this.Symbols.Contains(symbolToDelete) == false) {
				throw new Exception("ALREADY_DELETED[" + symbolToDelete + "] in [" + this.Name + "]");
			}
			this.Symbols.Remove(symbolToDelete);
			this.BarsRepository.SymbolDataFileDelete(symbolToDelete);
		}
		internal int BarAppend(Bar barLastFormed) {
			int ret = -1;
			if (this.ScaleInterval != barLastFormed.ScaleInterval) return ret;
			if (this.Symbols.Contains(barLastFormed.Symbol) == false) return ret;
			if (this.BarsRepository == null) return ret;
			ret = this.BarsRepository.DataFileForSymbol(barLastFormed.Symbol).BarAppend(barLastFormed);
			return ret;
		}
		public int BarsSave(Bars bars) {
			RepositoryBarsFile barsFile = this.BarsRepository.DataFileForSymbol(bars.Symbol, false);
			int barsSaved = barsFile.BarsSaveThreadSafe(bars);
			string msg = "Saved [ " + barsSaved + "] bars; static[" + this.Name + "]";

			//BarsFolder perstFolder = new BarsFolder(this.BarsFolder.RootFolder, bars.ScaleInterval, true, "dts");
			//RepositoryBarsPerst barsPerst = new RepositoryBarsPerst(perstFolder, bars.Symbol, false);
			//int barsSavedPerst = barsPerst.BarsSave(bars);
			//string msgPerst = "Saved [ " + barsSavedPerst + "] bars; static[" + this.Name + "]";
			return barsSaved;
		}

		// Initialize() creates the folder, now create empty files for non-file-existing-symbols
		internal int SyncDataFilesWithSymbols() {
			foreach (string symbolToAdd in this.Symbols) {
				if (this.BarsRepository.DataFileExistsForSymbol(symbolToAdd)) continue;
				this.BarsRepository.SymbolDataFileAdd(symbolToAdd);
			}
			List<string> symbolsToDelete = new List<string>();
			foreach (string symbolWhateverCase in this.BarsRepository.SymbolsInFolder) {
				string symbol = symbolWhateverCase.ToUpper();
				if (this.Symbols.Contains(symbol)) continue;
				symbolsToDelete.Add(symbol);
			}
			foreach (string symbolToDelete in symbolsToDelete) {
				this.BarsRepository.SymbolDataFileDelete(symbolToDelete);
			}
			return symbolsToDelete.Count;
		}
		internal void DataSourceFolderDeleteWithSymbols() {
			this.BarsRepository.DeleteAllDataFilesAllSymbols();
			Directory.Delete(this.DataSourceAbspath);
		}
		internal void DataSourceFolderRename(string newName) {
			string msig = " DataSourceFolderRename(" + this.Name + "=>" + newName + ")";
			if (File.Exists(this.DataSourceAbspath) == false) {
				throw new Exception("DATASOURCE_OLD_FOLDER_DOESNT_EXIST this.FolderForBarDataStore[" + this.DataSourceAbspath + "]" + msig);
			}
			string abspathNewFolderName = Path.Combine(this.BarsRepository.DataSourceAbspath, newName);
			if (File.Exists(abspathNewFolderName)) {
				throw new Exception("DATASOURCE_NEW_FOLDER_ALREADY_EXISTS abspathNewFolderName[" + abspathNewFolderName + "]" + msig);
			}
			Directory.Move(this.DataSourceAbspath, abspathNewFolderName);
			this.Name = newName;
			this.DataSourceAbspath = Path.Combine(this.DataSourcesAbspath, this.Name);
			this.BarsRepository = new RepositoryBarsSameScaleInterval(this.DataSourceAbspath, this.ScaleInterval, true);
		}
		public Bars BarsLoadAndCompress(string symbolRq, BarScaleInterval scaleIntervalRq) {
			Bars ret = this.RequestDataFromRepository(symbolRq);
			ret.DataSource = this;
			ret.MarketInfo = this.MarketInfo;
			ret.SymbolInfo = Assembler.InstanceInitialized.RepositoryCustomSymbolInfo.FindSymbolInfoOrNew(ret.Symbol);
			if (ret.Count == 0) return ret;
			if (scaleIntervalRq == ret.ScaleInterval) return ret;
			
			bool canConvert = ret.CanConvertTo(scaleIntervalRq);
			if (canConvert == false) {
				string msg = "CANNOT_COMPRESS_BARS " + symbolRq + "[" + ret.ScaleInterval + "]=>[" + scaleIntervalRq + "]";
				Assembler.PopupException(msg);
				return ret;
			}

			try {
				ret = ret.ToLargerScaleInterval(scaleIntervalRq);
			} catch (Exception e) {
				Assembler.PopupException("BARS_COMPRESSION_FAILED (ret, scaleIntervalRq)", e);
				throw e;
			}
			return ret;
		}
		public virtual Bars RequestDataFromRepository(string symbol) {
			Bars ret;
			symbol = symbol.ToUpper();

			//BarsFolder perstFolder = new BarsFolder(this.BarsFolder.RootFolder, this.DataSource.ScaleInterval, true, "dts");
			//RepositoryBarsPerst barsPerst = new RepositoryBarsPerst(perstFolder, symbol, false);
			//ret = barsPerst.BarsRead();
			//if (ret == null) {
			RepositoryBarsFile barsFile = this.BarsRepository.DataFileForSymbol(symbol);
			ret = barsFile.BarsLoadAllThreadSafe();
			//}
			if (ret == null) ret = new Bars(symbol, this.ScaleInterval, "FILE_NOT_FOUND " + this.GetType().Name);
			return ret;
		}
	}
}
