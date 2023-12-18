/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./components/**/*.{js,ts,jsx,tsx,mdx}",
    "./app/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  theme: {
    extend: {
      colors: {
        wall: "#18A44C",
        secondary_text: "#8D8D8D",
        button: "#17A14B",
        green_text: "#18A44C",
        orange_button: "#FFAD33",
        gray_text: "#706E6D",
      },
    },
  },
  plugins: [],
};
