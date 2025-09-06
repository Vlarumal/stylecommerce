export const categories = [
  { id: 'mens', name: "Men's Clothing", image: '/images/mens-clothing.jpg' },
  { id: 'womens', name: "Women's Clothing", image: '/images/womens-clothing.jpg' },
  { id: 'kids', name: "Kid's Clothing", image: '/images/kids-clothing.jpg' },
  { id: 'accessories', name: 'Accessories', image: '/images/accessories.jpg' },
  { id: 'footwear', name: 'Footwear', image: '/images/footwear.jpg' },
  { id: 'activewear', name: 'Activewear', image: '/images/activewear.jpg' },
];

// Category ID mapping for ProductList component
export const categoryIDMap: { [key: string]: number } = {
  'mens': 1,
  'womens': 2,
  'kids': 3,
  'accessories': 4,
  'footwear': 5,
  'activewear': 6
};