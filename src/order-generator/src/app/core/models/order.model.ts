export interface Order {
  ticker: string;
  side: 'BUY' | 'SELL';
  quantity: number;
  price: number;
}

export interface OrderResponse {
  status: string;
  message: string;
  orderId?: string;
  rejectReason?: string;
}
