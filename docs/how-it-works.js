/* ==========================================================================
   UnityUI Transformer — Interactive How-It-Works GSAP ScrollTrigger Engine
   ========================================================================== */

document.addEventListener('DOMContentLoaded', () => {
  // Register GSAP Plugins
  gsap.registerPlugin(ScrollTrigger);

  // 1. Page Header Entrance Animation
  gsap.from('.gsap-step-header', {
    duration: 0.9,
    y: 30,
    opacity: 0,
    stagger: 0.15,
    ease: 'power3.out'
  });

  // 2. Interactive Step-by-Step ScrollTrigger Timeline
  const stepCards = gsap.utils.toArray('.gsap-timeline-step');

  stepCards.forEach((step, index) => {
    // Reveal and focus active step as it reaches center viewport
    ScrollTrigger.create({
      trigger: step,
      start: 'top 75%',
      end: 'bottom 25%',
      toggleClass: { targets: step, className: 'active-step' },
      onEnter: () => {
        gsap.to(step, {
          opacity: 1,
          scale: 1,
          duration: 0.6,
          ease: 'power2.out'
        });
      },
      onLeave: () => {
        gsap.to(step, {
          opacity: 0.45,
          scale: 0.96,
          duration: 0.6,
          ease: 'power2.out'
        });
      },
      onEnterBack: () => {
        gsap.to(step, {
          opacity: 1,
          scale: 1,
          duration: 0.6,
          ease: 'power2.out'
        });
      },
      onLeaveBack: () => {
        if (index > 0) {
          gsap.to(step, {
            opacity: 0.45,
            scale: 0.96,
            duration: 0.6,
            ease: 'power2.out'
          });
        }
      }
    });

    // Initial entrance animation
    gsap.from(step, {
      scrollTrigger: {
        trigger: step,
        start: 'top 85%',
        toggleActions: 'play none none reverse'
      },
      duration: 0.9,
      y: 60,
      opacity: 0,
      ease: 'power3.out'
    });
  });

  // 3. Final Call to Action Reveal Animation
  gsap.from('.gsap-cta-card', {
    scrollTrigger: {
      trigger: '#download-cta',
      start: 'top 80%',
      toggleActions: 'play none none reverse'
    },
    duration: 1.0,
    scale: 0.9,
    y: 40,
    opacity: 0,
    ease: 'power3.out'
  });
});
