from sqlalchemy import create_engine, Column, Integer, String, Float, DateTime, Enum as SQLAlchemyEnum, ForeignKey, Text, BigInteger
from sqlalchemy.orm import relationship, declarative_base
from sqlalchemy.sql import func # For default timestamp
import datetime
from .enums import Exchange, TipType, ActionType # Import from local enums.py

Base = declarative_base()

class Stock(Base):
    __tablename__ = "stocks"

    id = Column(Integer, primary_key=True, index=True, autoincrement=True)
    symbol = Column(String(50), unique=True, index=True, nullable=False)
    name = Column(String(255), nullable=False)
    exchange = Column(SQLAlchemyEnum(Exchange), nullable=False)

    historical_prices = relationship("HistoricalPrice", back_populates="stock")
    live_prices = relationship("LivePrice", back_populates="stock")

    def __repr__(self):
        return f"<Stock(symbol='{self.symbol}', name='{self.name}', exchange='{self.exchange.value}')>"

class HistoricalPrice(Base):
    __tablename__ = "historical_prices"

    id = Column(Integer, primary_key=True, index=True, autoincrement=True)
    stock_id = Column(Integer, ForeignKey("stocks.id"), nullable=False)
    date = Column(DateTime, nullable=False) # Using DateTime for date for more flexibility, can be Date type too
    open = Column(Float(precision=2), nullable=False)
    high = Column(Float(precision=2), nullable=False)
    low = Column(Float(precision=2), nullable=False)
    close = Column(Float(precision=2), nullable=False)
    volume = Column(BigInteger, nullable=False)

    stock = relationship("Stock", back_populates="historical_prices")

    def __repr__(self):
        return f"<HistoricalPrice(stock_id={self.stock_id}, date='{self.date}', close={self.close})>"

class LivePrice(Base):
    __tablename__ = "live_prices"

    id = Column(Integer, primary_key=True, index=True, autoincrement=True)
    stock_id = Column(Integer, ForeignKey("stocks.id"), nullable=False)
    timestamp = Column(DateTime, default=datetime.datetime.utcnow, nullable=False)
    price = Column(Float(precision=2), nullable=False)
    volume = Column(BigInteger, nullable=False) # Volume for the tick/update

    stock = relationship("Stock", back_populates="live_prices")

    def __repr__(self):
        return f"<LivePrice(stock_id={self.stock_id}, timestamp='{self.timestamp}', price={self.price})>"

class SentimentData(Base):
    __tablename__ = "sentiment_data"

    id = Column(Integer, primary_key=True, index=True, autoincrement=True)
    source = Column(String(100), nullable=False) # e.g., Twitter, NewsAPI
    timestamp = Column(DateTime, default=datetime.datetime.utcnow, nullable=False)
    text = Column(Text, nullable=True) # Raw text
    sentiment_score = Column(Float(precision=2), nullable=False) # e.g., -1.0 to 1.0
    stock_symbol = Column(String(50), nullable=True, index=True) # Optional: if sentiment is specific to a stock

    def __repr__(self):
        return f"<SentimentData(source='{self.source}', stock_symbol='{self.stock_symbol}', score={self.sentiment_score})>"

class TradingTip(Base):
    __tablename__ = "trading_tips"

    id = Column(Integer, primary_key=True, index=True, autoincrement=True)
    timestamp = Column(DateTime, default=datetime.datetime.utcnow, nullable=False)
    stock_symbol = Column(String(50), nullable=False, index=True)
    tip_type = Column(SQLAlchemyEnum(TipType), nullable=False)
    action = Column(SQLAlchemyEnum(ActionType), nullable=False) # Renamed from action to action_type
    reason = Column(Text, nullable=False) # e.g., "High volume + positive global sentiment"
    confidence_score = Column(Float(precision=2), nullable=True) # e.g., 0.0 to 1.0

    def __repr__(self):
        return f"<TradingTip(stock_symbol='{self.stock_symbol}', action='{self.action.value}', type='{self.tip_type.value}')>"

# Example of how to create the engine and tables (usually in a database setup file)
# if __name__ == '__main__':
#     engine = create_engine('sqlite:///./test_models.db') # Example, use your actual DB URL
#     Base.metadata.create_all(bind=engine)
