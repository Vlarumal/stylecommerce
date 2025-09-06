FROM node:22-alpine

WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .

RUN npm run build

EXPOSE 5173
EXPOSE 4173

CMD ["npm", "run", "preview", "--", "--host", "0.0.0.0"]