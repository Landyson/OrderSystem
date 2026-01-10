CREATE DATABASE ordersystem1 CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE USER 'testUserDB'@'localhost' IDENTIFIED BY '1234';
GRANT ALL PRIVILEGES ON ordersystem.* TO 'testUserDB'@'localhost';
FLUSH PRIVILEGES;

use ordersystem1;

create table if not exists customers (
  id int auto_increment primary key,
  first_name varchar(80) not null,
  last_name varchar(80) not null,
  email varchar(255) not null unique,
  phone varchar(30) null,
  created_at datetime not null default current_timestamp
);

create table if not exists products (
  id int auto_increment primary key,
  name varchar(160) not null,
  price decimal(10,2) not null,
  stock int not null default 0,
  is_active tinyint(1) not null default 1, 
  rating float null,                        
  created_at datetime not null default current_timestamp,
  constraint uq_products_name unique (name)
);

create table if not exists orders (
  id int auto_increment primary key,
  customer_id int not null,
  state varchar(20) not null check (state in ('new','paid')), 
  note varchar(500) null,
  created_at datetime not null default current_timestamp,
  constraint fk_orders_customer foreign key (customer_id)
    references customers(id)
    on delete restrict on update cascade
);

create table if not exists order_items (
  order_id int not null,
  product_id int not null,
  quantity int not null,
  unit_price decimal(10,2) not null,
  primary key (order_id, product_id),
  constraint fk_items_order foreign key (order_id)
    references orders(id)
    on delete cascade on update cascade,
  constraint fk_items_product foreign key (product_id)
    references products(id)
    on delete restrict on update cascade
);

create table if not exists payments (
  id int auto_increment primary key,
  order_id int not null,
  amount decimal(10,2) not null,
  paid tinyint(1) not null default 0, 
  paid_at datetime null,
  provider varchar(60) null,
  constraint fk_payments_order foreign key (order_id)
    references orders(id)
    on delete cascade on update cascade
);

insert into customers(first_name,last_name,email,phone) values
('Jan','Novák','jan.novak@example.com','777111222')
on duplicate key update email=email;

insert into products(name,price,stock,is_active,rating) values('Tričko',399.00,50,1,4.2),
('Mikina',899.00,20,1,4.6),
('Čepice',249.00,35,1,3.9)
on duplicate key update name=name;


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