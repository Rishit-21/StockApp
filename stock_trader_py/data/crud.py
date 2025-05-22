from sqlalchemy.orm import Session
from typing import List, Optional, Type, TypeVar
from stock_trader_py.core import models, enums # Import all models and enums
import datetime

# Generic type for SQLAlchemy models
ModelType = TypeVar("ModelType", bound=models.Base)

# --- Generic CRUD Functions (Optional, can be used if preferred) ---
def get_by_id(db: Session, model: Type[ModelType], id: int) -> Optional[ModelType]:
    return db.query(model).filter(model.id == id).first()

def get_all(db: Session, model: Type[ModelType], skip: int = 0, limit: int = 100) -> List[ModelType]:
    return db.query(model).offset(skip).limit(limit).all()

def create_entity(db: Session, entity: models.Base) -> models.Base:
    db.add(entity)
    db.commit()
    db.refresh(entity)
    return entity

# --- Stock CRUD ---
def get_stock(db: Session, stock_id: int) -> Optional[models.Stock]:
    return db.query(models.Stock).filter(models.Stock.id == stock_id).first()

def get_stock_by_symbol(db: Session, symbol: str) -> Optional[models.Stock]:
    return db.query(models.Stock).filter(models.Stock.symbol == symbol.upper()).first()

def get_stocks(db: Session, skip: int = 0, limit: int = 100) -> List[models.Stock]:
    return db.query(models.Stock).offset(skip).limit(limit).all()

def create_stock(db: Session, symbol: str, name: str, exchange: enums.Exchange) -> models.Stock:
    db_stock = models.Stock(symbol=symbol.upper(), name=name, exchange=exchange)
    db.add(db_stock)
    db.commit()
    db.refresh(db_stock)
    return db_stock

# --- HistoricalPrice CRUD ---
def create_historical_price(db: Session, stock_id: int, date: datetime.datetime, open_price: float, high_price: float, low_price: float, close_price: float, volume: int) -> models.HistoricalPrice:
    db_price = models.HistoricalPrice(
        stock_id=stock_id, date=date, open=open_price, high=high_price, low=low_price, close=close_price, volume=volume
    )
    db.add(db_price)
    # Commit can be done in batch by the service layer if adding many prices
    # db.commit()
    # db.refresh(db_price)
    return db_price

def add_historical_prices_bulk(db: Session, prices: List[models.HistoricalPrice]):
    db.add_all(prices)
    db.commit() # Commit after adding all prices in the list

def get_historical_prices(db: Session, stock_id: int, start_date: datetime.datetime, end_date: datetime.datetime, skip: int = 0, limit: int = 1000) -> List[models.HistoricalPrice]:
    return db.query(models.HistoricalPrice).filter(
        models.HistoricalPrice.stock_id == stock_id,
        models.HistoricalPrice.date >= start_date,
        models.HistoricalPrice.date <= end_date
    ).order_by(models.HistoricalPrice.date).offset(skip).limit(limit).all()

# --- LivePrice CRUD ---
def create_live_price(db: Session, stock_id: int, price: float, volume: int, timestamp: Optional[datetime.datetime] = None) -> models.LivePrice:
    db_price = models.LivePrice(
        stock_id=stock_id, price=price, volume=volume, timestamp=timestamp or datetime.datetime.utcnow()
    )
    db.add(db_price)
    # db.commit() # Usually commit after a batch or logical operation
    # db.refresh(db_price)
    return db_price
    
def add_live_prices_bulk(db: Session, prices: List[models.LivePrice]):
    db.add_all(prices)
    db.commit()

def get_latest_live_price(db: Session, stock_id: int) -> Optional[models.LivePrice]:
    return db.query(models.LivePrice).filter(models.LivePrice.stock_id == stock_id).order_by(models.LivePrice.timestamp.desc()).first()


# --- SentimentData CRUD ---
def create_sentiment_data(db: Session, source: str, text: Optional[str], sentiment_score: float, stock_symbol: Optional[str] = None, timestamp: Optional[datetime.datetime] = None) -> models.SentimentData:
    db_sentiment = models.SentimentData(
        source=source, text=text, sentiment_score=sentiment_score, stock_symbol=stock_symbol.upper() if stock_symbol else None, timestamp=timestamp or datetime.datetime.utcnow()
    )
    db.add(db_sentiment)
    return db_sentiment

def add_sentiment_data_bulk(db: Session, sentiments: List[models.SentimentData]):
    db.add_all(sentiments)
    db.commit()

def get_sentiment_data(db: Session, stock_symbol: Optional[str] = None, start_date: Optional[datetime.datetime] = None, end_date: Optional[datetime.datetime] = None, limit: int = 100) -> List[models.SentimentData]:
    query = db.query(models.SentimentData)
    if stock_symbol:
        query = query.filter(models.SentimentData.stock_symbol == stock_symbol.upper())
    if start_date:
        query = query.filter(models.SentimentData.timestamp >= start_date)
    if end_date:
        query = query.filter(models.SentimentData.timestamp <= end_date)
    return query.order_by(models.SentimentData.timestamp.desc()).limit(limit).all()

# --- TradingTip CRUD ---
def create_trading_tip(db: Session, stock_symbol: str, tip_type: enums.TipType, action: enums.ActionType, reason: str, confidence_score: Optional[float] = None, timestamp: Optional[datetime.datetime] = None) -> models.TradingTip:
    db_tip = models.TradingTip(
        stock_symbol=stock_symbol.upper(), tip_type=tip_type, action=action, reason=reason, confidence_score=confidence_score, timestamp=timestamp or datetime.datetime.utcnow()
    )
    db.add(db_tip)
    return db_tip

def add_trading_tips_bulk(db: Session, tips: List[models.TradingTip]):
    db.add_all(tips)
    db.commit()

def get_trading_tips(db: Session, stock_symbol: Optional[str] = None, limit: int = 20, page: int = 1) -> List[models.TradingTip]:
    query = db.query(models.TradingTip)
    if stock_symbol:
        query = query.filter(models.TradingTip.stock_symbol == stock_symbol.upper())
    
    offset = (page - 1) * limit
    return query.order_by(models.TradingTip.timestamp.desc()).offset(offset).limit(limit).all()
