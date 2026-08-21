/* ==========================================================================
   UnityUI Transformer — GSAP Timeline & ScrollTrigger Engine
   ========================================================================== */

document.addEventListener('DOMContentLoaded', () => {
  // Register GSAP Plugins
  gsap.registerPlugin(ScrollTrigger);

  // 1. Master Hero Entrance Timeline
  const heroTimeline = gsap.timeline({ defaults: { ease: 'power3.out' } });

  heroTimeline
    // Step 1: Navigation fade-in from top
    .from('.navbar', {
      duration: 0.9,
      y: -30,
      opacity: 0
    })
    
    // Step 2: Hero Headline Scale-Up Reveal ("Transform Figma into Production UI")
    .from('.hero-title', {
      duration: 1.1,
      scale: 0.88,
      y: 35,
      opacity: 0
    }, '-=0.4')
    
    // Step 3: Stagger Subtitle & Pill Tag Reveal
    .from('.pill-tag', {
      duration: 0.6,
      y: 20,
      opacity: 0
    }, '-=0.7')
    .from('.hero-subtitle', {
      duration: 0.8,
      y: 20,
      opacity: 0
    }, '-=0.5')
    .from('.hero-ctas', {
      duration: 0.8,
      y: 20,
      opacity: 0
    }, '-=0.5')
    
    // Step 4: Floating 3D CSS Geometry Entrance
    .from('.gsap-hero-cube', {
      duration: 1.2,
      scale: 0.6,
      opacity: 0,
      ease: 'back.out(1.6)'
    }, '-=0.9');

  // 2. Navbar Scroll Glassmorphism Toggle
  const navbar = document.getElementById('navbar');
  window.addEventListener('scroll', () => {
    if (window.scrollY > 40) {
      navbar.classList.add('scrolled');
    } else {
      navbar.classList.remove('scrolled');
    }
  });

  // 3. Feature Cards Stagger Scroll Reveal
  gsap.from('.feature-card', {
    scrollTrigger: {
      trigger: '.features',
      start: 'top 75%',
      toggleActions: 'play none none reverse'
    },
    duration: 0.9,
    y: 50,
    opacity: 0,
    stagger: 0.15,
    ease: 'power3.out'
  });
});
