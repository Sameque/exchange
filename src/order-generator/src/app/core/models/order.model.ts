export interface Order {
  symbol: string;
  side: 'Compra' | 'Venda';
  quantity: number;
  price: number;
}

export interface OrderResponse {
  success: boolean;
  message: string;
  orderId?: string;
  errorReason?: string;
}
