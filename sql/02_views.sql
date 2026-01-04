use ordersystem;

create or replace view v_order_totals as
select
  o.id as order_id,
  o.customer_id,
  concat(c.first_name, ' ', c.last_name) as customer_name,
  o.state as status,
  o.created_at,
  coalesce(sum(oi.quantity * oi.unit_price), 0) as total_amount
from orders o
join customers c on c.id = o.customer_id
left join order_items oi on oi.order_id = o.id
group by o.id, o.customer_id, customer_name, o.state, o.created_at;

create or replace view v_product_sales as
select
  p.id as product_id,  p.name,
  coalesce(sum(oi.quantity), 0) as qty_sold,
  coalesce(sum(oi.quantity * oi.unit_price), 0) as revenue
from products p
left join order_items oi on oi.product_id = p.id
left join orders o on o.id = oi.order_id
group by p.id, p.name;
